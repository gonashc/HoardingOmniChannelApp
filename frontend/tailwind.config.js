/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      fontFamily: {
        // Refined fintech: distinctive humanist serif for display, precise grotesque for body, true mono for data
        display: ['"Tiempos Headline"', '"Source Serif 4"', 'Georgia', 'serif'],
        sans: ['"Inter Tight"', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'monospace']
      },
      colors: {
        // Off-white canvas, deep ink — soft neutrals only
        canvas:  '#FAFAF7',
        surface: '#FFFFFF',
        ink: {
          50:  '#F5F5F1',
          100: '#EBEBE4',
          200: '#D6D6CB',
          300: '#A8A89B',
          400: '#6E6E60',
          500: '#4A4A3E',
          600: '#34342B',
          700: '#22221C',
          800: '#15150F',
          900: '#0B0B07'
        },
        // Single restrained accent — deep indigo, used sparingly for action
        accent: {
          50:  '#EEF1F8',
          100: '#DCE2F0',
          400: '#5566A3',
          500: '#2E3F7C',
          600: '#23315F',
          700: '#1A2548'
        },
        signal: {
          positive: { 50: '#F0F7F2', 500: '#2F6B41' },
          warning:  { 50: '#FBF6E9', 500: '#8B6A1A' },
          negative: { 50: '#FAEFEC', 500: '#9C3A26' }
        }
      },
      fontSize: {
        '2xs':  ['0.6875rem', { lineHeight: '1rem',     letterSpacing: '0.04em' }],
        'xs':   ['0.75rem',   { lineHeight: '1.125rem', letterSpacing: '0.015em' }],
        'sm':   ['0.8125rem', { lineHeight: '1.25rem' }],
        'base': ['0.9375rem', { lineHeight: '1.5rem' }],
        'lg':   ['1.0625rem', { lineHeight: '1.625rem' }],
        'xl':   ['1.25rem',   { lineHeight: '1.75rem',  letterSpacing: '-0.01em' }],
        '2xl':  ['1.5rem',    { lineHeight: '2rem',     letterSpacing: '-0.015em' }],
        '3xl':  ['1.875rem',  { lineHeight: '2.25rem',  letterSpacing: '-0.02em' }],
        '4xl':  ['2.5rem',    { lineHeight: '1.1',      letterSpacing: '-0.025em' }],
        '5xl':  ['3.5rem',    { lineHeight: '1.05',     letterSpacing: '-0.03em' }],
        '6xl':  ['4.5rem',    { lineHeight: '1.0',      letterSpacing: '-0.035em' }]
      },
      letterSpacing: { eyebrow: '0.12em' },
      borderRadius: {
        DEFAULT: '4px',
        md: '6px',
        lg: '10px',
        xl: '14px'
      },
      boxShadow: {
        edge:      '0 0 0 1px rgb(11 11 7 / 0.06)',
        card:      '0 1px 0 rgb(11 11 7 / 0.04), 0 0 0 1px rgb(11 11 7 / 0.06)',
        cardHover: '0 4px 12px rgb(11 11 7 / 0.05), 0 0 0 1px rgb(11 11 7 / 0.10)',
        focus:     '0 0 0 3px rgb(46 63 124 / 0.18)'
      },
      maxWidth: { layout: '1280px' },
      transitionDuration: { DEFAULT: '180ms' }
    }
  },
  plugins: []
};
