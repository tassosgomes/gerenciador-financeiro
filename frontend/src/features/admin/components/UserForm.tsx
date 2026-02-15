import { useCallback, useMemo, useState } from 'react';
import { createUserSchema } from '@/features/admin/schemas/userSchema';
import { RoleType } from '@/features/admin/types/admin';
import { useCreateUser } from '@/features/admin/hooks/useUsers';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui';

interface UserFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

function getPasswordStrength(password: string): { strength: number; label: string; color: string } {
  if (!password) return { strength: 0, label: '', color: '' };

  let strength = 0;
  if (password.length >= 8) strength++;
  if (password.length >= 12) strength++;
  if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++;
  if (/[0-9]/.test(password)) strength++;
  if (/[^a-zA-Z0-9]/.test(password)) strength++;

  if (strength <= 2) return { strength, label: 'Fraca', color: 'bg-red-500' };
  if (strength === 3) return { strength, label: 'Média', color: 'bg-yellow-500' };
  return { strength, label: 'Forte', color: 'bg-green-500' };
}

export function UserForm({ open, onOpenChange }: UserFormProps): JSX.Element {
  const createMutation = useCreateUser();

  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>(RoleType.Member);
  const [errors, setErrors] = useState<{ name?: string; email?: string; password?: string; role?: string }>({});

  const passwordStrength = useMemo(() => getPasswordStrength(password), [password]);

  const resetForm = useCallback(() => {
    setName('');
    setEmail('');
    setPassword('');
    setSelectedRole(RoleType.Member);
    setErrors({});
  }, []);

  function handleOpenChange(newOpen: boolean): void {
    if (newOpen) {
      // Reset form when opening dialog
      resetForm();
    }
    onOpenChange(newOpen);
  }

  function validate(): boolean {
    const newErrors: { name?: string; email?: string; password?: string; role?: string } = {};

    const result = createUserSchema.safeParse({
      name,
      email,
      password,
      role: selectedRole,
    });

    if (!result.success) {
      result.error.issues.forEach((issue) => {
        const field = issue.path[0] as keyof typeof newErrors;
        if (field && !newErrors[field]) {
          newErrors[field] = issue.message;
        }
      });
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  async function handleSubmit(e: React.FormEvent): Promise<void> {
    e.preventDefault();

    if (!validate()) return;

    try {
      await createMutation.mutateAsync({
        name,
        email,
        password,
        role: selectedRole as RoleType,
      });
      onOpenChange(false);
    } catch {
      // Error already handled by mutation hook
    }
  }

  const isLoading = createMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Novo Usuário</DialogTitle>
          <DialogDescription>
            Preencha os dados para criar um novo usuário. Uma senha temporária será criada.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          {/* Nome */}
          <div className="space-y-2">
            <label htmlFor="name" className="text-sm font-medium">
              Nome <span className="text-red-500">*</span>
            </label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ex: João Silva"
              className={errors.name ? 'border-red-500' : ''}
              disabled={isLoading}
            />
            {errors.name && <p className="text-sm text-red-500">{errors.name}</p>}
          </div>

          {/* E-mail */}
          <div className="space-y-2">
            <label htmlFor="email" className="text-sm font-medium">
              E-mail <span className="text-red-500">*</span>
            </label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Ex: joao@email.com"
              className={errors.email ? 'border-red-500' : ''}
              disabled={isLoading}
            />
            {errors.email && <p className="text-sm text-red-500">{errors.email}</p>}
          </div>

          {/* Senha Temporária */}
          <div className="space-y-2">
            <label htmlFor="password" className="text-sm font-medium">
              Senha Temporária <span className="text-red-500">*</span>
            </label>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Mínimo 8 caracteres"
              className={errors.password ? 'border-red-500' : ''}
              disabled={isLoading}
            />
            {password && (
              <div className="mt-2">
                <div className="flex gap-1 mb-1">
                  {[...Array(5)].map((_, i) => (
                    <div
                      key={i}
                      className={`h-1 flex-1 rounded ${i < passwordStrength.strength ? passwordStrength.color : 'bg-gray-200'}`}
                    />
                  ))}
                </div>
                <p className={`text-xs ${passwordStrength.strength <= 2 ? 'text-red-600' : passwordStrength.strength === 3 ? 'text-yellow-600' : 'text-green-600'}`}>
                  Força da senha: {passwordStrength.label}
                </p>
              </div>
            )}
            {errors.password && <p className="text-sm text-red-500">{errors.password}</p>}
          </div>

          {/* Papel */}
          <div className="space-y-2">
            <label htmlFor="role" className="text-sm font-medium">
              Papel <span className="text-red-500">*</span>
            </label>
            <Select
              value={selectedRole}
              onValueChange={(value) => setSelectedRole(value)}
              disabled={isLoading}
            >
              <SelectTrigger id="role" className={errors.role ? 'border-red-500' : ''}>
                <SelectValue placeholder="Selecione o papel" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={RoleType.Admin}>Administrador</SelectItem>
                <SelectItem value={RoleType.Member}>Membro</SelectItem>
              </SelectContent>
            </Select>
            {errors.role && <p className="text-sm text-red-500">{errors.role}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isLoading}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Criando...' : 'Criar'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
