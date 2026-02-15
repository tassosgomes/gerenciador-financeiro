import type { Config } from 'tailwindcss';
import forms from '@tailwindcss/forms';

const config: Config = {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#137fec',
          dark: '#0e62b6',
        },
        success: '#10b981',
        danger: '#ef4444',
        warning: '#f59e0b',
        background: {
          light: '#f6f7f8',
          dark: '#101922',
        },
        surface: {
          light: '#ffffff',
          dark: '#182430',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        DEFAULT: '0.25rem',
        lg: '0.5rem',
        xl: '0.75rem',
        '2xl': '1rem',
      },
    },
  },
  plugins: [forms],
};

export default config;
