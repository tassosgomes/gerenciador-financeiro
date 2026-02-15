import { useState } from 'react';
import { useForm } from 'react-hook-form';

import { loginSchema, type LoginFormValues } from '@/features/auth/schemas/loginSchema';
import { useAuthStore } from '@/features/auth/store/authStore';
import { Button, Input } from '@/shared/components/ui';

interface LoginFormProps {
  onSuccess?: () => void;
}

export function LoginForm({ onSuccess }: LoginFormProps): JSX.Element {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const login = useAuthStore((state) => state.login);
  const isLoading = useAuthStore((state) => state.isLoading);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const emailValidation = loginSchema.shape.email;
  const passwordValidation = loginSchema.shape.password;

  async function onSubmit(values: LoginFormValues): Promise<void> {
    setErrorMessage(null);

    try {
      await login(values.email, values.password);
      onSuccess?.();
    } catch {
      setErrorMessage('Credenciais inválidas');
    }
  }

  return (
    <form className="space-y-6" onSubmit={handleSubmit(onSubmit)}>
      {errorMessage ? (
        <div
          className="rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-sm text-danger"
          role="alert"
        >
          {errorMessage}
        </div>
      ) : null}

      <div>
        <label className="mb-1.5 block text-sm font-medium text-slate-700" htmlFor="email">
          E-mail
        </label>
        <div className="relative">
          <span className="material-icons pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-lg text-slate-400">
            mail_outline
          </span>
          <Input
            autoComplete="email"
            className="h-11 border-slate-300 pl-10"
            id="email"
            placeholder="exemplo@email.com"
            type="email"
            {...register('email', {
              validate: (value) => {
                const parsed = emailValidation.safeParse(value);
                return parsed.success || parsed.error.issues[0]?.message;
              },
            })}
          />
        </div>
        {errors.email?.message ? (
          <p className="mt-1 text-sm text-danger" role="alert">
            {errors.email.message}
          </p>
        ) : null}
      </div>

      <div>
        <label className="mb-1.5 block text-sm font-medium text-slate-700" htmlFor="password">
          Senha
        </label>
        <div className="relative">
          <span className="material-icons pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-lg text-slate-400">
            lock_outline
          </span>
          <Input
            autoComplete="current-password"
            className="h-11 border-slate-300 pl-10"
            id="password"
            placeholder="********"
            type="password"
            {...register('password', {
              validate: (value) => {
                const parsed = passwordValidation.safeParse(value);
                return parsed.success || parsed.error.issues[0]?.message;
              },
            })}
          />
        </div>
        {errors.password?.message ? (
          <p className="mt-1 text-sm text-danger" role="alert">
            {errors.password.message}
          </p>
        ) : null}
      </div>

      <div className="flex items-center justify-between">
        <label className="flex cursor-not-allowed items-center gap-2 text-sm text-slate-400" htmlFor="remember-me">
          <input
            className="h-4 w-4 rounded border-slate-300 text-primary focus:ring-primary"
            disabled
            id="remember-me"
            type="checkbox"
          />
          Lembrar de mim
        </label>

        <button className="text-sm font-medium text-slate-400" disabled type="button">
          Esqueceu a senha?
        </button>
      </div>

      <Button className="h-11 w-full bg-primary text-white hover:bg-primary-dark" disabled={isLoading} type="submit">
        {isLoading ? 'Entrando...' : 'Entrar'}
      </Button>
    </form>
  );
}
