import { HttpMethod } from './http-request.model';

export interface QuickAction {
  label: string;
  method: HttpMethod;
  path: string;
  body?: string;
}

export const QUICK_ACTIONS: ReadonlyArray<QuickAction> = [
  { label: 'List cats', method: 'GET', path: '/cats' },
  { label: 'Get cat #1', method: 'GET', path: '/cats/1' },
  {
    label: 'Create cat',
    method: 'POST',
    path: '/cats',
    body: JSON.stringify({ name: 'Simba', breed: 'Bengal', age: 4 }, null, 2),
  },
  {
    label: 'Update cat #1',
    method: 'PUT',
    path: '/cats/1',
    body: JSON.stringify({ name: 'Luna', breed: 'Siberian', age: 4 }, null, 2),
  },
  { label: 'Delete cat #1', method: 'DELETE', path: '/cats/1' },
  { label: 'List cookies', method: 'GET', path: '/cookies' },
  {
    label: 'Set cookie',
    method: 'POST',
    path: '/cookies',
    body: JSON.stringify({ name: 'session', value: 'abc123' }, null, 2),
  },
  { label: 'Static index', method: 'GET', path: '/' },
];
