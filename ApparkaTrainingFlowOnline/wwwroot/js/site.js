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

document.querySelectorAll('form[data-confirm]').forEach(form => {
    form.addEventListener('submit', event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});

document.querySelectorAll('[data-learning-module]').forEach(module => {
    const steps = [...module.querySelectorAll('[data-learning-step]')];
    const previous = module.querySelector('[data-learning-previous]');
    const next = module.querySelector('[data-learning-next]');
    const complete = module.querySelector('[data-learning-complete]');
    const progress = module.querySelector('[data-learning-progress]');
    const currentLabel = module.querySelector('[data-learning-current]');
    let current = 0;

    const render = () => {
        steps.forEach((step, index) => {
            const active = index === current;
            step.hidden = !active;
            step.classList.toggle('active', active);
        });

        previous.disabled = current === 0;
        next.hidden = current === steps.length - 1;
        complete.hidden = current !== steps.length - 1;
        currentLabel.textContent = String(current + 1);
        progress.style.width = `${((current + 1) / steps.length) * 100}%`;
        module.scrollIntoView({ behavior: 'smooth', block: 'start' });
    };

    previous.addEventListener('click', () => {
        if (current > 0) { current--; render(); }
    });
    next.addEventListener('click', () => {
        if (current < steps.length - 1) { current++; render(); }
    });
    render();
});
