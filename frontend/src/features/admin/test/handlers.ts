import { http, HttpResponse } from 'msw';

import type { CreateUserRequest, UserResponse } from '@/features/admin/types/admin';
import { RoleType } from '@/features/admin/types/admin';

const mockUsers: UserResponse[] = [
  {
    id: '1',
    name: 'Admin User',
    email: 'admin@example.com',
    role: RoleType.Admin,
    isActive: true,
    createdAt: '2026-01-01T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    name: 'João Silva',
    email: 'joao@example.com',
    role: RoleType.Member,
    isActive: true,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '3',
    name: 'Maria Santos',
    email: 'maria@example.com',
    role: RoleType.Member,
    isActive: false,
    createdAt: '2026-01-20T10:00:00Z',
    updatedAt: '2026-02-01T10:00:00Z',
  },
];

const BASE_URL = '*';

export const adminHandlers = [
  // GET /api/v1/users
  http.get(`${BASE_URL}/api/v1/users`, () => {
    return HttpResponse.json(mockUsers);
  }),

  // POST /api/v1/users
  http.post(`${BASE_URL}/api/v1/users`, async ({ request }) => {
    const body = (await request.json()) as CreateUserRequest;

    const newUser: UserResponse = {
      id: String(mockUsers.length + 1),
      name: body.name,
      email: body.email,
      role: body.role,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    mockUsers.push(newUser);
    return HttpResponse.json(newUser, { status: 201 });
  }),

  // PATCH /api/v1/users/:id/status
  http.patch(`${BASE_URL}/api/v1/users/:id/status`, async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as { isActive: boolean };
    const userIndex = mockUsers.findIndex((user) => user.id === id);

    if (userIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    mockUsers[userIndex] = {
      ...mockUsers[userIndex],
      isActive: body.isActive,
      updatedAt: new Date().toISOString(),
    };

    return new HttpResponse(null, { status: 204 });
  }),

  // GET /api/v1/backup/export
  http.get(`${BASE_URL}/api/v1/backup/export`, () => {
    const backupData = {
      exportedAt: new Date().toISOString(),
      version: '1.0',
      data: {
        users: mockUsers,
        accounts: [],
        categories: [],
        transactions: [],
      },
    };

    const blob = new Blob([JSON.stringify(backupData, null, 2)], {
      type: 'application/json',
    });

    return new HttpResponse(blob, {
      headers: {
        'Content-Type': 'application/json',
        'Content-Disposition': 'attachment; filename="backup.json"',
      },
    });
  }),

  // POST /api/v1/backup/import
  http.post(`${BASE_URL}/api/v1/backup/import`, async () => {
    // Simulate processing time
    await new Promise((resolve) => setTimeout(resolve, 1000));

    return new HttpResponse(null, { status: 204 });
  }),
];
