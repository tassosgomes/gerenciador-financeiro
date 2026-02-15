import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';

import { formatCurrency } from '@/shared/utils';

interface DonutChartDataPoint {
  name: string;
  value: number;
  color?: string;
}

interface DonutChartWidgetProps {
  data: DonutChartDataPoint[];
  height?: number;
}

const CHART_COLORS = ['#137fec', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6'];

export function DonutChartWidget({ data, height = 280 }: DonutChartWidgetProps): JSX.Element {
  return (
    <ResponsiveContainer height={height} width="100%">
      <PieChart>
        <Pie
          cx="40%"
          cy="50%"
          data={data}
          dataKey="value"
          innerRadius={55}
          outerRadius={90}
          paddingAngle={3}
        >
          {data.map((entry, index) => (
            <Cell key={entry.name} fill={entry.color ?? CHART_COLORS[index % CHART_COLORS.length]} />
          ))}
        </Pie>
        <Tooltip formatter={(value: number) => formatCurrency(value)} />
        <Legend align="right" layout="vertical" verticalAlign="middle" />
      </PieChart>
    </ResponsiveContainer>
  );
}
