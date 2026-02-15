import { http, HttpResponse } from 'msw';

import type { CategoryResponse, CreateCategoryRequest, UpdateCategoryRequest } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';

const mockCategories: CategoryResponse[] = [
  {
    id: '1',
    name: 'Alimentação',
    type: CategoryType.Expense,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    name: 'Transporte',
    type: CategoryType.Expense,
    createdAt: '2026-01-16T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '3',
    name: 'Salário',
    type: CategoryType.Income,
    createdAt: '2026-01-17T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '4',
    name: 'Freelance',
    type: CategoryType.Income,
    createdAt: '2026-01-18T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '5',
    name: 'Moradia',
    type: CategoryType.Expense,
    createdAt: '2026-01-19T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '6',
    name: 'Lazer',
    type: CategoryType.Expense,
    createdAt: '2026-01-20T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '7',
    name: 'Investimento',
    type: CategoryType.Income,
    createdAt: '2026-01-21T10:00:00Z',
    updatedAt: null,
  },
];

const BASE_URL = 'http://localhost:5000';

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

    mockCategories[categoryIndex] = {
      ...mockCategories[categoryIndex],
      name: body.name,
      updatedAt: new Date().toISOString(),
    };

    return HttpResponse.json(mockCategories[categoryIndex]);
  }),
];
