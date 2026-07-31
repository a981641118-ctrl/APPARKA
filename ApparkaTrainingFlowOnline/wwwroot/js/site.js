window.copyField = async function (id) {
    const field = document.getElementById(id);
    if (!field) return;
    try {
        await navigator.clipboard.writeText(field.value);
        const original = field.nextElementSibling.textContent;
        field.nextElementSibling.textContent = "Copiado";
        setTimeout(() => field.nextElementSibling.textContent = original, 1600);
    } catch {
        field.select();
        document.execCommand("copy");
    }
};

document.querySelectorAll('.toast').forEach(el => setTimeout(() => {
    el.style.opacity = '0';
    el.style.transform = 'translateY(-6px)';
    setTimeout(() => el.remove(), 250);
}, 5000));

if ('serviceWorker' in navigator) { window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js')); }
