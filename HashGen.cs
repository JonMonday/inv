using System;
using BCrypt.Net;

class Program {
    static void Main() {
        string hash = BCrypt.Net.BCrypt.HashPassword("admin123");
        Console.WriteLine(hash);
    }
}
