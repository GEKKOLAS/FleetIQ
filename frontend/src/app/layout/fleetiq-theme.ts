/**
 * FleetIQ brand theme. One teal primary and two surface ramps: a cool grey for the light mode and
 * the navy of the control-tower 3D scene for the dark mode, so the app and the scene share one look.
 */
export const BRAND_PRIMARY = {
    50: '#ecfbf6', 100: '#d0f5e9', 200: '#a4ead5', 300: '#6bd8bd', 400: '#2fc2a3', 500: '#0e8a75',
    600: '#0b7463', 700: '#0b5d50', 800: '#0b4a41', 900: '#0a3d36', 950: '#04241f'
};

export const SURFACE_LIGHT = {
    0: '#ffffff', 50: '#f6f9fb', 100: '#eef3f7', 200: '#dde6ec', 300: '#c5d2dc', 400: '#93a6b5', 500: '#65798a',
    600: '#4a5d6e', 700: '#354656', 800: '#1f2f3d', 900: '#13212d', 950: '#0a151e'
};

export const SURFACE_DARK = {
    0: '#ffffff', 50: '#f1f6f9', 100: '#dfe9ef', 200: '#c2d3dd', 300: '#9bb3c2', 400: '#7391a3', 500: '#547285',
    600: '#3b5769', 700: '#24404f', 800: '#162f40', 900: '#0f2433', 950: '#091a26'
};

const STORAGE_KEY = 'fleetiq.theme';

/** Saved choice first, then the operating-system preference. Storage can be unavailable (private mode). */
export function initialDarkTheme(): boolean {
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved === 'dark' || saved === 'light') return saved === 'dark';
    } catch { /* fall through to the system preference */ }
    return typeof matchMedia === 'function' && matchMedia('(prefers-color-scheme: dark)').matches;
}

export function rememberDarkTheme(dark: boolean) {
    try { localStorage.setItem(STORAGE_KEY, dark ? 'dark' : 'light'); } catch { /* not persisted */ }
}
