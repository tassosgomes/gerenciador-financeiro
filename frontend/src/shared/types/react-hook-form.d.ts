declare module 'react-hook-form' {
  export interface FieldValues {
    [key: string]: unknown;
  }

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
    register: (name: keyof TFieldValues & string, options?: RegisterOptions) => UseFormRegisterReturn;
    handleSubmit: (
      onValid: (values: TFieldValues) => Promise<void> | void,
    ) => (event?: unknown) => Promise<void>;
    formState: {
      errors: Partial<Record<keyof TFieldValues, ValidationResult>>;
    };
  }

  export function useForm<TFieldValues extends FieldValues = FieldValues>(options?: {
    defaultValues?: Partial<TFieldValues>;
  }): UseFormReturn<TFieldValues>;
}
