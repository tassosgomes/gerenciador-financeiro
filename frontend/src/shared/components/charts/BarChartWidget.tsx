import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

import { formatCurrency } from '@/shared/utils';

interface BarChartDataPoint {
  label: string;
  revenue: number;
  expense: number;
}

interface BarChartWidgetProps {
  data: BarChartDataPoint[];
  height?: number;
}

export function BarChartWidget({ data, height = 280 }: BarChartWidgetProps): JSX.Element {
  return (
    <ResponsiveContainer height={height} width="100%">
      <BarChart data={data}>
        <CartesianGrid stroke="#e2e8f0" strokeDasharray="3 3" />
        <XAxis dataKey="label" tick={{ fill: '#64748b', fontSize: 12 }} />
        <YAxis
          tick={{ fill: '#64748b', fontSize: 12 }}
          tickFormatter={(value) => formatCurrency(Number(value))}
        />
        <Tooltip formatter={(value: number) => formatCurrency(value)} />
        <Legend formatter={(value) => (value === 'revenue' ? 'Receita' : 'Despesa')} />
        <Bar dataKey="revenue" fill="#137fec" name="Receita" radius={[4, 4, 0, 0]} />
        <Bar dataKey="expense" fill="#94a3b8" name="Despesa" radius={[4, 4, 0, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
