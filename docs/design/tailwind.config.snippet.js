// Paste into _Layout.cshtml after the Tailwind CDN script:
// <script>tailwind.config = { ...below }</script>
tailwind.config = {
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    screens: { sm: '640px', lg: '1024px', xl: '1440px' },
    extend: {
      colors: {
        bg: 'var(--bg)',
        surface: 'var(--surface)',
        sunken: 'var(--surface-sunken)',
        brand: 'var(--surface-brand)',
        line: 'var(--line)',
        'line-strong': 'var(--line-strong)',
        ink: 'var(--ink)',
        muted: 'var(--ink-muted)',
        subtle: 'var(--ink-subtle)',
        'on-brand': 'var(--on-brand)',
        'on-brand-muted': 'var(--on-brand-muted)',
        primary: 'var(--primary)',
        'primary-hover': 'var(--primary-hover)',
        'on-primary': 'var(--on-primary)',
        chip: 'var(--chip-bg)',
        accent: 'var(--accent)',
        'accent-soft': 'var(--accent-soft)',
        'accent-ink': 'var(--accent-ink)',
        'success-ink': 'var(--success-ink)',
        'success-soft': 'var(--success-soft)',
        'danger-ink': 'var(--danger-ink)',
        'danger-soft': 'var(--danger-soft)',
        danger: 'var(--danger-fill)'
      },
      fontFamily: {
        sans: ['IBM Plex Sans Arabic', 'Segoe UI', 'Tahoma', 'system-ui', 'sans-serif'],
        display: ['Aref Ruqaa', 'Amiri', 'serif'],
        reading: ['Amiri', 'serif']
      },
      borderRadius: { sm: '6px', md: '10px', lg: '16px', xl: '24px' },
      boxShadow: { sm: 'var(--shadow-sm)', md: 'var(--shadow-md)', lg: 'var(--shadow-lg)' },
      width: { sidebar: '264px', rail: '72px' },
      height: { topbar: '56px', bottombar: '64px', touch: '44px' },
      maxWidth: { content: '1280px' }
    }
  }
};
