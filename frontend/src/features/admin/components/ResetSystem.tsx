import { useState } from 'react';
import { AlertTriangle } from 'lucide-react';

import { useResetSystem } from '@/features/admin/hooks/useSystem';
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
} from '@/shared/components/ui';

export function ResetSystem(): JSX.Element {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [confirmationText, setConfirmationText] = useState('');
  const resetMutation = useResetSystem();

  const handleReset = () => {
    resetMutation.mutate();
    setIsDialogOpen(false);
    setConfirmationText('');
  };

  const handleOpenDialog = () => {
    setIsDialogOpen(true);
    setConfirmationText('');
  };

  const handleCloseDialog = () => {
    setIsDialogOpen(false);
    setConfirmationText('');
  };

  const isConfirmationValid = confirmationText === 'CONFIRMO';

  return (
    <>
      <Card className="border-red-200 bg-red-50/50">
        <CardHeader>
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-red-600" />
            <CardTitle className="text-red-900">Resetar Sistema</CardTitle>
          </div>
          <CardDescription className="text-red-700">
            Exclui permanentemente todas as transações, contas e categorias personalizadas
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="rounded-lg bg-white border border-red-200 p-4">
              <p className="text-sm font-semibold text-red-900 mb-2">
                ⚠️ Esta ação irá excluir:
              </p>
              <ul className="space-y-1 text-sm text-red-800">
                <li className="flex items-center gap-2">
                  <span className="material-icons text-xs">close</span>
                  Todas as transações
                </li>
                <li className="flex items-center gap-2">
                  <span className="material-icons text-xs">close</span>
                  Todas as contas (Cartão, Investimento, etc)
                </li>
                <li className="flex items-center gap-2">
                  <span className="material-icons text-xs">close</span>
                  Todas as categorias personalizadas
                </li>
              </ul>
              <p className="text-sm font-semibold text-green-700 mt-3 mb-1">
                ✓ Serão mantidos:
              </p>
              <ul className="space-y-1 text-sm text-green-700">
                <li className="flex items-center gap-2">
                  <span className="material-icons text-xs">check</span>
                  Todos os usuários
                </li>
                <li className="flex items-center gap-2">
                  <span className="material-icons text-xs">check</span>
                  Categorias do sistema
                </li>
              </ul>
            </div>

            <Button
              onClick={handleOpenDialog}
              variant="destructive"
              className="w-full"
              disabled={resetMutation.isPending}
            >
              <AlertTriangle className="mr-2 h-4 w-4" />
              Resetar Sistema
            </Button>
          </div>
        </CardContent>
      </Card>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-red-600">
              <AlertTriangle className="h-5 w-5" />
              Confirmação de Reset
            </DialogTitle>
            <DialogDescription className="space-y-3 pt-2">
              <p className="font-semibold text-red-600">
                ⚠️ ATENÇÃO: Esta ação é IRREVERSÍVEL!
              </p>
              <p className="text-sm">
                Todas as transações, contas e categorias personalizadas serão{' '}
                <strong>EXCLUÍDAS PERMANENTEMENTE</strong>.
              </p>
              <p className="text-sm font-semibold">
                Recomendamos fazer um backup antes de continuar!
              </p>
              <p className="text-sm">
                Para confirmar, digite <strong className="text-red-600">CONFIRMO</strong> no campo abaixo:
              </p>
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2">
            <p className="text-sm font-medium">Confirmação</p>
            <Input
              id="confirmation"
              value={confirmationText}
              onChange={(e) => setConfirmationText(e.target.value)}
              placeholder="Digite CONFIRMO"
              className="text-center font-mono text-lg"
              autoFocus
            />
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={handleCloseDialog}
              disabled={resetMutation.isPending}
            >
              Cancelar
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleReset}
              disabled={!isConfirmationValid || resetMutation.isPending}
            >
              {resetMutation.isPending ? (
                <>
                  <span className="material-icons mr-2 animate-spin text-base">sync</span>
                  Resetando...
                </>
              ) : (
                <>
                  <AlertTriangle className="mr-2 h-4 w-4" />
                  Confirmar Reset
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
