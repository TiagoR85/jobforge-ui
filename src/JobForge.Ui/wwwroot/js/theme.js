function getTheme() {
    return localStorage.getItem('theme') || 'dark';
}

function setTheme(theme) {
    localStorage.setItem('theme', theme);
    if (theme === 'dark') {
        document.documentElement.classList.add('dark');
    } else {
        document.documentElement.classList.remove('dark');
    }
}

function initTheme() {
    const theme = getTheme();
    if (theme === 'dark') {
        document.documentElement.classList.add('dark');
    } else {
        document.documentElement.classList.remove('dark');
    }
    return theme;
}

function toggleTheme() {
    const current = getTheme();
    const next = current === 'light' ? 'dark' : 'light';
    setTheme(next);
    return next;
}

function getAccent() {
    return localStorage.getItem('accent') || 'blue';
}

function setAccent(accent) {
    localStorage.setItem('accent', accent);
    const accents = {
        emerald: { primary: 'oklch(0.55 0.19 160)', ring: 'oklch(0.55 0.19 160)', sidebarPrimary: 'oklch(0.55 0.19 160)' },
        blue: { primary: 'oklch(0.55 0.19 240)', ring: 'oklch(0.55 0.19 240)', sidebarPrimary: 'oklch(0.55 0.19 240)' },
        violet: { primary: 'oklch(0.55 0.19 280)', ring: 'oklch(0.55 0.19 280)', sidebarPrimary: 'oklch(0.55 0.19 280)' },
        rose: { primary: 'oklch(0.55 0.19 350)', ring: 'oklch(0.55 0.19 350)', sidebarPrimary: 'oklch(0.55 0.19 350)' },
        orange: { primary: 'oklch(0.55 0.19 50)', ring: 'oklch(0.55 0.19 50)', sidebarPrimary: 'oklch(0.55 0.19 50)' },
        slate: { primary: 'oklch(0.55 0.02 260)', ring: 'oklch(0.55 0.02 260)', sidebarPrimary: 'oklch(0.55 0.02 260)' }
    };
    const colors = accents[accent] || accents.blue;
    document.documentElement.style.setProperty('--primary', colors.primary);
    document.documentElement.style.setProperty('--ring', colors.ring);
    document.documentElement.style.setProperty('--sidebar-primary', colors.sidebarPrimary);
    document.documentElement.style.setProperty('--chart-1', colors.primary);
}

function initAccent() {
    const accent = getAccent();
    setAccent(accent);
    return accent;
}

function addKeydownListener(dotNetHelper) {
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('Open');
        }
    });
}

function getSidebarCollapsed() {
    return localStorage.getItem('sidebarCollapsed') === 'true';
}

function setSidebarCollapsed(collapsed) {
    localStorage.setItem('sidebarCollapsed', collapsed.toString());
}

function getIsMobile() {
    return window.innerWidth < 768;
}

let _resizeHandler = null;
let _resizeDotNetRef = null;

function addResizeListener(dotNetHelper) {
    removeResizeListener();
    _resizeDotNetRef = dotNetHelper;
    _resizeHandler = () => {
        if (_resizeDotNetRef) {
            _resizeDotNetRef.invokeMethodAsync('OnResize', getIsMobile()).catch(() => {});
        }
    };
    window.addEventListener('resize', _resizeHandler);
}

function removeResizeListener() {
    if (_resizeHandler) {
        window.removeEventListener('resize', _resizeHandler);
        _resizeHandler = null;
        _resizeDotNetRef = null;
    }
}
