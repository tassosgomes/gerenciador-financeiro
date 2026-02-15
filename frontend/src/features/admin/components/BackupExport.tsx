import { Download } from 'lucide-react';

import { useExportBackup } from '@/features/admin/hooks/useBackup';
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui';

export function BackupExport(): JSX.Element {
  const exportMutation = useExportBackup();

  const handleExport = () => {
    exportMutation.mutate();
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <span className="material-icons text-primary">download</span>
          <CardTitle>Exportar Backup</CardTitle>
        </div>
        <CardDescription>
          Faça o download de todos os dados do sistema em formato JSON
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="rounded-lg bg-slate-50 p-4">
            <p className="text-sm text-slate-600">
              O backup contém:
            </p>
            <ul className="mt-2 space-y-1 text-sm text-slate-600">
              <li className="flex items-center gap-2">
                <span className="material-icons text-xs">check</span>
                Todas as contas e saldos
              </li>
              <li className="flex items-center gap-2">
                <span className="material-icons text-xs">check</span>
                Categorias cadastradas
              </li>
              <li className="flex items-center gap-2">
                <span className="material-icons text-xs">check</span>
                Histórico completo de transações
              </li>
              <li className="flex items-center gap-2">
                <span className="material-icons text-xs">check</span>
                Usuários do sistema
              </li>
            </ul>
          </div>

          <Button
            onClick={handleExport}
            disabled={exportMutation.isPending}
            className="w-full"
          >
            {exportMutation.isPending ? (
              <>
                <span className="material-icons mr-2 animate-spin text-base">sync</span>
                Exportando...
              </>
            ) : (
              <>
                <Download className="mr-2 h-4 w-4" />
                Exportar Agora
              </>
            )}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
