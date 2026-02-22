declare module 'react-hook-form' {
  import type { ReactElement } from 'react';

  export interface FieldValues {
    [key: string]: unknown;
  }

  export type FieldPath<TFieldValues extends FieldValues> = keyof TFieldValues & string;

  export interface ValidationResult {
    message?: string;
  }

  export interface RegisterOptions {
    validate?: (value: string) => boolean | string | undefined;
  }

  export interface UseFormRegisterReturn {
    name: string;
    onChange: (event: unknown) => void;
    onBlur: (event: unknown) => void;
    ref: (instance: unknown) => void;
  }

  export interface UseFormReturn<TFieldValues extends FieldValues> {
    register: (name: FieldPath<TFieldValues>, options?: RegisterOptions) => UseFormRegisterReturn;
    control: unknown;
    watch: <TName extends FieldPath<TFieldValues>>(name: TName) => TFieldValues[TName];
    setValue: <TName extends FieldPath<TFieldValues>>(name: TName, value: TFieldValues[TName]) => void;
    getValues: <TName extends FieldPath<TFieldValues>>(name: TName) => TFieldValues[TName];
    reset: (values?: Partial<TFieldValues>) => void;
    handleSubmit: (
      onValid: (values: TFieldValues) => Promise<void> | void,
    ) => (event?: unknown) => Promise<void>;
    formState: {
      errors: Partial<Record<keyof TFieldValues, ValidationResult>>;
    };
  }

  export function useForm<TFieldValues extends FieldValues = FieldValues>(options?: {
    defaultValues?: Partial<TFieldValues>;
    resolver?: unknown;
  }): UseFormReturn<TFieldValues>;

  export interface ControllerFieldState {
    invalid: boolean;
  }

  export interface ControllerRenderProps<TFieldValues extends FieldValues, TName extends FieldPath<TFieldValues>> {
    name: TName;
    value: TFieldValues[TName];
    onChange: (value: TFieldValues[TName]) => void;
    onBlur: () => void;
    ref: (instance: unknown) => void;
  }

  export interface ControllerProps<TFieldValues extends FieldValues, TName extends FieldPath<TFieldValues>> {
    name: TName;
    control: unknown;
    render: (props: {
      field: ControllerRenderProps<TFieldValues, TName>;
      fieldState: ControllerFieldState;
    }) => ReactElement;
  }

  export function Controller<
    TFieldValues extends FieldValues = FieldValues,
    TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>,
  >(props: ControllerProps<TFieldValues, TName>): ReactElement;
}
