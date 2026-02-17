import { Fragment, type ReactNode } from 'react';

import { SelectGroup, SelectItem, SelectLabel, SelectSeparator } from './select';

interface OptionBase {
  id: string;
  name: string;
  type: number;
}

interface CategorySelectOptionGroupsProps<T extends OptionBase> {
  items: T[];
  expenseType: number;
  incomeType: number;
  expenseLabel?: string;
  incomeLabel?: string;
  includeLeadingSeparator?: boolean;
  getItemLabel?: (item: T) => ReactNode;
}

interface AccountSelectOptionGroupsProps<T extends OptionBase> {
  items: T[];
  typeLabels: Record<number, string>;
  orderedTypes?: number[];
  includeLeadingSeparator?: boolean;
  getItemLabel?: (item: T) => ReactNode;
}

function sortByName<T extends OptionBase>(items: T[]): T[] {
  return [...items].sort((left, right) => left.name.localeCompare(right.name));
}

export function CategorySelectOptionGroups<T extends OptionBase>({
  items,
  expenseType,
  incomeType,
  expenseLabel = 'Despesas',
  incomeLabel = 'Receitas',
  includeLeadingSeparator = false,
  getItemLabel,
}: CategorySelectOptionGroupsProps<T>): JSX.Element {
  const expenses = sortByName(items.filter((item) => item.type === expenseType));
  const incomes = sortByName(items.filter((item) => item.type === incomeType));

  return (
    <>
      {includeLeadingSeparator && (expenses.length > 0 || incomes.length > 0) && <SelectSeparator className="my-2" />}

      {expenses.length > 0 && (
        <SelectGroup>
          <SelectLabel>{expenseLabel}</SelectLabel>
          {expenses.map((item) => (
            <SelectItem key={item.id} value={item.id}>
              {getItemLabel?.(item) ?? item.name}
            </SelectItem>
          ))}
        </SelectGroup>
      )}

      {expenses.length > 0 && incomes.length > 0 && <SelectSeparator />}

      {incomes.length > 0 && (
        <SelectGroup>
          <SelectLabel>{incomeLabel}</SelectLabel>
          {incomes.map((item) => (
            <SelectItem key={item.id} value={item.id}>
              {getItemLabel?.(item) ?? item.name}
            </SelectItem>
          ))}
        </SelectGroup>
      )}
    </>
  );
}

export function AccountSelectOptionGroups<T extends OptionBase>({
  items,
  typeLabels,
  orderedTypes,
  includeLeadingSeparator = false,
  getItemLabel,
}: AccountSelectOptionGroupsProps<T>): JSX.Element {
  const presentTypes = [...new Set(items.map((item) => item.type))];
  const orderedPresentTypes = orderedTypes
    ? [
        ...orderedTypes.filter((type) => presentTypes.includes(type)),
        ...presentTypes
          .filter((type) => !orderedTypes.includes(type))
          .sort((left, right) => (typeLabels[left] ?? String(left)).localeCompare(typeLabels[right] ?? String(right))),
      ]
    : presentTypes.sort((left, right) => (typeLabels[left] ?? String(left)).localeCompare(typeLabels[right] ?? String(right)));

  return (
    <>
      {orderedPresentTypes.map((type, index) => {
        const groupedItems = sortByName(items.filter((item) => item.type === type));
        if (groupedItems.length === 0) return null;

        return (
          <Fragment key={type}>
            {index === 0
              ? includeLeadingSeparator && <SelectSeparator className="my-2" />
              : <SelectSeparator />}
            <SelectGroup>
              <SelectLabel>{typeLabels[type] ?? String(type)}</SelectLabel>
              {groupedItems.map((item) => (
                <SelectItem key={item.id} value={item.id}>
                  {getItemLabel?.(item) ?? item.name}
                </SelectItem>
              ))}
            </SelectGroup>
          </Fragment>
        );
      })}
    </>
  );
}
