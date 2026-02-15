import { Button, Card, CardContent, CardHeader, CardTitle, Input } from '@/shared/components/ui';

export default function LoginPage(): JSX.Element {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background-light p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">GestorFinanceiro</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <Input placeholder="E-mail" type="email" />
          <Input placeholder="Senha" type="password" />
          <Button className="w-full" type="button">
            Entrar
          </Button>
        </CardContent>
      </Card>
    </main>
  );
}
