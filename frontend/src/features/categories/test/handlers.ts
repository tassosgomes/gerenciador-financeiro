import { http, HttpResponse } from 'msw';

import type { CategoryResponse, CreateCategoryRequest, UpdateCategoryRequest } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';

const mockCategories: CategoryResponse[] = [
  {
    id: '1',
    name: 'Alimentação',
    type: CategoryType.Expense,
    isSystem: true,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    name: 'Transporte',
    type: CategoryType.Expense,
    isSystem: true,
    createdAt: '2026-01-16T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '3',
    name: 'Salário',
    type: CategoryType.Income,
    isSystem: true,
    createdAt: '2026-01-17T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '4',
    name: 'Freelance',
    type: CategoryType.Income,
    isSystem: false,
    createdAt: '2026-01-18T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '5',
    name: 'Moradia',
    type: CategoryType.Expense,
    isSystem: true,
    createdAt: '2026-01-19T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '6',
    name: 'Lazer',
    type: CategoryType.Expense,
    isSystem: false,
    createdAt: '2026-01-20T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '7',
    name: 'Investimento',
    type: CategoryType.Income,
    isSystem: false,
    createdAt: '2026-01-21T10:00:00Z',
    updatedAt: null,
  },
];

const BASE_URL = '*';

export const categoriesHandlers = [
  // GET /api/v1/categories
  http.get(`${BASE_URL}/api/v1/categories`, ({ request }) => {
    const url = new URL(request.url);
    const typeParam = url.searchParams.get('type');

    let filteredCategories = mockCategories;
    if (typeParam) {
      const type = Number(typeParam);
      filteredCategories = mockCategories.filter((cat) => cat.type === type);
    }

    return HttpResponse.json(filteredCategories);
  }),

  // POST /api/v1/categories
  http.post(`${BASE_URL}/api/v1/categories`, async ({ request }) => {
    const body = (await request.json()) as CreateCategoryRequest;

    const newCategory: CategoryResponse = {
      id: String(mockCategories.length + 1),
      name: body.name,
      type: body.type,
      isSystem: false,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    mockCategories.push(newCategory);
    return HttpResponse.json(newCategory, { status: 201 });
  }),

  // PUT /api/v1/categories/:id
  http.put(`${BASE_URL}/api/v1/categories/:id`, async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as UpdateCategoryRequest;
    const categoryIndex = mockCategories.findIndex((cat) => cat.id === id);

    if (categoryIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    // Bloquear edição de categorias do sistema
    if (mockCategories[categoryIndex].isSystem) {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc9457#section-3',
          title: 'Categoria do sistema não pode ser alterada',
          status: 400,
          detail: 'Categorias do sistema não podem ser editadas ou removidas.',
        },
        { status: 400 }
      );
    }

    mockCategories[categoryIndex] = {
      ...mockCategories[categoryIndex],
      name: body.name,
      updatedAt: new Date().toISOString(),
    };

    return HttpResponse.json(mockCategories[categoryIndex]);
  }),

  // DELETE /api/v1/categories/:id
  http.delete(`${BASE_URL}/api/v1/categories/:id`, ({ params, request }) => {
    const { id } = params;
    const url = new URL(request.url);
    const migrateToCategoryId = url.searchParams.get('migrateToCategoryId');
    const categoryIndex = mockCategories.findIndex((cat) => cat.id === id);

    if (categoryIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    if (mockCategories[categoryIndex].isSystem) {
      return HttpResponse.json(
        {
          type: 'https://httpstatuses.com/400',
          title: 'Categoria do sistema não pode ser alterada',
          status: 400,
          detail: 'Categorias do sistema não podem ser editadas ou removidas.',
        },
        { status: 400 }
      );
    }

    if (!migrateToCategoryId) {
      return HttpResponse.json(
        {
          type: 'https://httpstatuses.com/409',
          title: 'Categoria em uso',
          status: 409,
          detail: 'Category has linked records. Choose a target category to migrate before deletion.',
        },
        { status: 409 }
      );
    }

    mockCategories.splice(categoryIndex, 1);
    return new HttpResponse(null, { status: 204 });
  }),
];
