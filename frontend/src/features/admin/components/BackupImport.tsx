import { useState, useRef } from 'react';
import { Upload, FileJson, AlertTriangle } from 'lucide-react';

import { useImportBackup } from '@/features/admin/hooks/useBackup';
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui';
import { ConfirmationModal } from '@/shared/components/ui/ConfirmationModal';

export function BackupImport(): JSX.Element {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const importMutation = useImportBackup();

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      const file = e.dataTransfer.files[0];
      if (file.type === 'application/json' || file.name.endsWith('.json')) {
        setSelectedFile(file);
      }
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      if (file.type === 'application/json' || file.name.endsWith('.json')) {
        setSelectedFile(file);
      }
    }
  };

  const handleButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleImportClick = () => {
    if (selectedFile) {
      setShowConfirmModal(true);
    }
  };

  const handleConfirmImport = () => {
    if (selectedFile) {
      importMutation.mutate(selectedFile, {
        onSuccess: () => {
          setShowConfirmModal(false);
          setSelectedFile(null);
        },
      });
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <span className="material-icons text-primary">upload</span>
            <CardTitle>Importar Backup</CardTitle>
          </div>
          <CardDescription>
            Restaure os dados do sistema a partir de um arquivo JSON
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {/* Warning */}
            <div className="flex gap-3 rounded-lg bg-yellow-50 p-4 border border-yellow-200">
              <AlertTriangle className="h-5 w-5 flex-shrink-0 text-yellow-600" />
              <div>
                <p className="text-sm font-medium text-yellow-800">
                  Atenção: A importação substituirá TODOS os dados existentes.
                </p>
                <p className="mt-1 text-sm text-yellow-700">
                  Esta ação é irreversível. Recomendamos exportar um backup antes de continuar.
                </p>
              </div>
            </div>

            {/* Drop Zone */}
            <div
              onDragEnter={handleDrag}
              onDragLeave={handleDrag}
              onDragOver={handleDrag}
              onDrop={handleDrop}
              className={`relative rounded-lg border-2 border-dashed p-8 text-center transition-colors ${
                dragActive
                  ? 'border-primary bg-primary/5'
                  : 'border-slate-300 hover:border-slate-400'
              }`}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept=".json,application/json"
                onChange={handleFileChange}
                className="hidden"
              />

              {selectedFile ? (
                <div className="space-y-3">
                  <FileJson className="mx-auto h-12 w-12 text-primary" />
                  <div>
                    <p className="font-medium text-slate-900">{selectedFile.name}</p>
                    <p className="text-sm text-slate-500">{formatFileSize(selectedFile.size)}</p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setSelectedFile(null)}
                  >
                    Remover
                  </Button>
                </div>
              ) : (
                <div className="space-y-3">
                  <Upload className="mx-auto h-12 w-12 text-slate-400" />
                  <div>
                    <p className="text-sm font-medium text-slate-700">
                      Arraste o arquivo JSON aqui
                    </p>
                    <p className="text-sm text-slate-500">ou clique no botão abaixo</p>
                  </div>
                  <Button
                    variant="outline"
                    onClick={handleButtonClick}
                    type="button"
                  >
                    Selecionar Arquivo
                  </Button>
                </div>
              )}
            </div>

            {/* Import Button */}
            <Button
              onClick={handleImportClick}
              disabled={!selectedFile || importMutation.isPending}
              className="w-full"
              variant="destructive"
            >
              {importMutation.isPending ? (
                <>
                  <span className="material-icons mr-2 animate-spin text-base">sync</span>
                  Importando...
                </>
              ) : (
                <>
                  <Upload className="mr-2 h-4 w-4" />
                  Importar Backup
                </>
              )}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Confirmation Modal */}
      <ConfirmationModal
        open={showConfirmModal}
        onOpenChange={setShowConfirmModal}
        onConfirm={handleConfirmImport}
        onCancel={() => setShowConfirmModal(false)}
        title="Confirmar Importação"
        message={`Tem certeza que deseja importar o backup "${selectedFile?.name}"? Todos os dados atuais serão substituídos e esta ação não pode ser desfeita.`}
        confirmLabel="Importar"
        variant="danger"
      />
    </>
  );
}
