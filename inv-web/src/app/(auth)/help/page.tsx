'use client';

import { Search, ChevronRight } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';

export default function HelpPage() {
    const faqs = [
        {
            question: 'How do I create a new inventory request?',
            answer: 'Navigate to Requests > New Request. Select your warehouse, department, and workflow template, then add products and quantities. Submit the request for approval.'
        },
        {
            question: 'How do I approve or reject a task?',
            answer: 'Go to My Tasks to see pending approvals. Click on a task to view details, then use the Approve or Reject buttons. You can add comments to explain your decision.'
        },
        {
            question: 'What is a workflow template?',
            answer: 'Workflow templates define the approval process for requests. They specify who needs to approve, in what order, and what actions are available at each step.'
        },
        {
            question: 'How do I fulfill an inventory request?',
            answer: 'As a storekeeper, go to Fulfillment to see pending requests. Click on a request and use the Reserve, Release, or Issue actions to process the inventory movement.'
        },
        {
            question: 'How can I track inventory movements?',
            answer: 'Navigate to Inventory > Movements to see a complete audit trail of all stock movements, including issues, receipts, adjustments, and transfers.'
        },
        {
            question: 'How do I add new users to the system?',
            answer: 'Admins can go to Admin > Users and click "Add User". Fill in the user details, assign roles, and set their initial password.'
        },
        {
            question: 'What reports are available?',
            answer: 'Go to Reports Center to access inventory reports, request analytics, and audit logs. You can export data in CSV or JSON format.'
        },
        {
            question: 'How do I reset my password?',
            answer: 'Contact your system administrator to reset your password. They can set a new temporary password from the Admin > Users section.'
        }
    ];

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Help & FAQ</h1>
                    <p className="text-sm text-muted-foreground">Find answers to common questions</p>
                </div>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Search Help Topics</CardTitle>
                    <CardDescription>Type a keyword to find relevant help articles</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="relative">
                        <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                        <Input placeholder="Search for help..." className="pl-9" />
                    </div>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Frequently Asked Questions</CardTitle>
                    <CardDescription>Common questions and answers about using InvServer</CardDescription>
                </CardHeader>
                <CardContent>
                    <Accordion type="single" collapsible className="w-full">
                        {faqs.map((faq, index) => (
                            <AccordionItem key={index} value={`item-${index}`}>
                                <AccordionTrigger className="text-left">
                                    {faq.question}
                                </AccordionTrigger>
                                <AccordionContent className="text-sm text-muted-foreground">
                                    {faq.answer}
                                </AccordionContent>
                            </AccordionItem>
                        ))}
                    </Accordion>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Quick Links</CardTitle>
                    <CardDescription>Helpful resources and guides</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="space-y-2">
                        <a href="#" className="flex items-center justify-between p-3 rounded-md hover:bg-accent transition-colors">
                            <span className="text-sm font-medium">Getting Started Guide</span>
                            <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        </a>
                        <a href="#" className="flex items-center justify-between p-3 rounded-md hover:bg-accent transition-colors">
                            <span className="text-sm font-medium">Video Tutorials</span>
                            <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        </a>
                        <a href="#" className="flex items-center justify-between p-3 rounded-md hover:bg-accent transition-colors">
                            <span className="text-sm font-medium">API Documentation</span>
                            <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        </a>
                        <a href="#" className="flex items-center justify-between p-3 rounded-md hover:bg-accent transition-colors">
                            <span className="text-sm font-medium">Contact Support</span>
                            <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        </a>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
