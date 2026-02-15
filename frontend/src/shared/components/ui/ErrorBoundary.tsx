import { Component, type ErrorInfo, type PropsWithChildren, type ReactNode } from 'react';
import { AlertCircle } from 'lucide-react';

import { Button } from '@/shared/components/ui/button';
import { Card, CardContent } from '@/shared/components/ui/card';

interface ErrorBoundaryProps extends PropsWithChildren {
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    this.props.onError?.(error, errorInfo);
  }

  handleReset = (): void => {
    this.setState({ hasError: false, error: null });
  };

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="flex items-center justify-center min-h-[400px] p-8" role="alert" aria-live="assertive">
          <Card className="max-w-md w-full">
            <CardContent className="pt-6">
              <div className="flex flex-col items-center text-center space-y-4">
                <AlertCircle className="h-16 w-16 text-red-500" aria-hidden="true" />
                <div className="space-y-2">
                  <h2 className="text-xl font-bold text-slate-900">Algo deu errado</h2>
                  <p className="text-sm text-slate-600">
                    Ocorreu um erro inesperado ao carregar esta página.
                  </p>
                  {import.meta.env.DEV && this.state.error && (
                    <details className="text-left mt-4">
                      <summary className="cursor-pointer text-sm font-medium text-slate-700">
                        Detalhes do erro (apenas em desenvolvimento)
                      </summary>
                      <pre className="mt-2 text-xs text-red-600 overflow-auto max-h-32 p-2 bg-red-50 rounded">
                        {this.state.error.toString()}
                      </pre>
                    </details>
                  )}
                </div>
                <Button onClick={this.handleReset} className="mt-4">
                  Tentar novamente
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      );
    }

    return this.props.children;
  }
}
