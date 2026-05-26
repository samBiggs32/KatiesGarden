window.CartStorage = {
    get: () => localStorage.getItem('kg-cart') || '[]',
    set: (json) => localStorage.setItem('kg-cart', json),
    clear: () => localStorage.removeItem('kg-cart')
};

window.ClipboardHelper = {
    copy: (text) => navigator.clipboard.writeText(text)
};
