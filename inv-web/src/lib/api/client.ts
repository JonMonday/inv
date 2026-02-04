import axios from 'axios';
import { useAuthStore } from '@/store/authStore';
import { v4 as uuidv4 } from 'uuid';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5119';

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true, // Required for HttpOnly refresh cookie
});

// Request interceptor: Attach Token & Idempotency Key
apiClient.interceptors.request.use(
    (config) => {
        const { accessToken } = useAuthStore.getState();
        if (accessToken) {
            config.headers.Authorization = `Bearer ${accessToken}`;
        }

        // Add Idempotency Key for fulfillment endpoints
        if (config.url?.includes('/fulfillment/')) {
            config.headers['X-Idempotency-Key'] = uuidv4();
        }

        return config;
    },
    (error) => Promise.reject(error)
);

// Response interceptor: Handle 401 Refresh
let isRefreshing = false;
let failedQueue: { resolve: (t: string | null) => void, reject: (err: Error) => void }[] = [];

const processQueue = (error: Error | null, token: string | null = null) => {
    failedQueue.forEach((prom: { resolve: (t: string | null) => void, reject: (err: Error) => void }) => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });
    failedQueue = [];
};

apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                    .then((token) => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return apiClient(originalRequest);
                    })
                    .catch((err) => Promise.reject(err));
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                const refreshResponse = await axios.post(`${API_BASE_URL}/api/auth/refresh`, {}, { withCredentials: true });
                const { accessToken } = refreshResponse.data.data;

                useAuthStore.getState().updateAccessToken(accessToken);
                processQueue(null, accessToken);

                originalRequest.headers.Authorization = `Bearer ${accessToken}`;
                return apiClient(originalRequest);
            } catch (refreshError: unknown) {
                processQueue(refreshError as Error, null);
                useAuthStore.getState().clearAuth();
                // window.location.href = '/login'; // Handle redirect in a more React-friendly way if possible
                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        return Promise.reject(error);
    }
);

export default apiClient;
