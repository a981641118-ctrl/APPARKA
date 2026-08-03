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

document.querySelectorAll('[data-password-toggle]').forEach(button => {
    button.addEventListener('click', () => {
        const input = button.closest('.password-field')?.querySelector('input');
        if (!input) return;
        const willShow = input.type === 'password';
        input.type = willShow ? 'text' : 'password';
        button.classList.toggle('is-visible', willShow);
        button.setAttribute('aria-pressed', String(willShow));
        button.setAttribute('aria-label', willShow ? 'Ocultar contraseña' : 'Mostrar contraseña');
        button.title = willShow ? 'Ocultar contraseña' : 'Mostrar contraseña';
    });
});

document.querySelectorAll('[data-supervisor-review]').forEach(form => {
    const rows = [...form.querySelectorAll('[data-rubric-row]')];
    const overallAssessment = form.querySelector('[data-overall-assessment]');
    const observedStrength = form.querySelector('[data-observed-strength]');
    const strengthLabel = form.querySelector('[data-strength-label]');
    const evidenceLabel = form.querySelector('[data-overall-evidence-label]');
    const improvementField = form.querySelector('[data-improvement-field]');
    const improvementInput = improvementField?.querySelector('textarea');
    const suggestion = form.querySelector('[data-evaluation-suggestion]');
    const noStrengthOption = observedStrength?.querySelector('option[value="6"]');

    const updateSuggestion = () => {
        const ratings = rows.map(row => Number(row.querySelector('[data-rubric-rating]')?.value ?? 0));
        if (ratings.some(value => value === 0)) {
            suggestion.textContent = 'Completa primero los cinco criterios para recibir una orientación del sistema.';
            return;
        }
        if (ratings.every(value => value === 1)) {
            suggestion.textContent = 'La rúbrica muestra cumplimiento total. Puedes elegir “Cumplimiento esperado” o “Desempeño destacado” si observaste un rendimiento superior al estándar.';
        } else if (ratings.some(value => value === 3) || ratings.filter(value => value === 2).length >= 2) {
            suggestion.textContent = 'La rúbrica presenta incumplimientos relevantes. Revisa si corresponde seleccionar “Requiere mejora”.';
        } else {
            suggestion.textContent = 'La rúbrica muestra un cumplimiento mayoritario con una oportunidad puntual de mejora. Confirma el resultado según lo observado.';
        }
    };

    const updateRubricRow = row => {
        const rating = row.querySelector('[data-rubric-rating]');
        const details = row.querySelector('[data-rubric-details]');
        if (!rating || !details) return;
        const requiresDetail = rating.value === '2' || rating.value === '3';
        details.hidden = !requiresDetail;
        details.querySelectorAll('textarea').forEach(field => {
            field.required = requiresDetail;
            if (!requiresDetail) field.value = '';
        });
        rating.setCustomValidity(rating.value === '0' ? 'Selecciona una calificación.' : '');
        updateSuggestion();
    };

    const updateOverall = () => {
        if (!overallAssessment || !observedStrength) return;
        const needsImprovement = overallAssessment.value === '3';
        overallAssessment.setCustomValidity(overallAssessment.value === '0' ? 'Selecciona el resultado general.' : '');
        noStrengthOption.disabled = !needsImprovement;
        noStrengthOption.hidden = !needsImprovement;
        if (!needsImprovement && observedStrength.value === '6') observedStrength.value = '0';
        observedStrength.setCustomValidity(observedStrength.value === '0' ? 'Selecciona el aspecto principal.' : '');
        improvementField.hidden = !needsImprovement;
        improvementInput.required = needsImprovement;
        if (!needsImprovement) improvementInput.value = '';
        strengthLabel.textContent = needsImprovement ? 'Principal fortaleza identificada' : 'Principal fortaleza o aspecto correcto';
        evidenceLabel.textContent = needsImprovement ? 'Evidencia que sustenta la evaluación' : 'Ejemplo concreto de la fortaleza seleccionada';
    };

    rows.forEach(row => {
        row.querySelector('[data-rubric-rating]')?.addEventListener('change', () => updateRubricRow(row));
        updateRubricRow(row);
    });
    overallAssessment?.addEventListener('change', updateOverall);
    observedStrength?.addEventListener('change', updateOverall);
    form.addEventListener('submit', () => {
        rows.forEach(updateRubricRow);
        updateOverall();
    });
    updateOverall();
});
