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

document.querySelectorAll('[data-location-supervisor-link]').forEach(group => {
    const locationSelect = group.querySelector('[data-location-select]');
    const supervisorSelect = group.querySelector('[data-supervisor-select]');
    const help = group.querySelector('[data-location-supervisor-help]');
    if (!locationSelect || !supervisorSelect) return;

    const locationOptions = [...locationSelect.options].map(option => option.cloneNode(true));
    const supervisorOptions = [...supervisorSelect.options].map(option => option.cloneNode(true));

    const replaceOptions = (select, source, predicate, selectedValue, emptyText) => {
        const options = source.filter(option => !option.value || predicate(option)).map(option => option.cloneNode(true));
        const emptyOption = options.find(option => !option.value);
        if (emptyOption) emptyOption.textContent = emptyText;
        select.replaceChildren(...options);
        const canRestore = options.some(option => option.value === selectedValue);
        select.value = canRestore ? selectedValue : '';
    };

    const locationsForSupervisor = supervisorId => {
        const option = supervisorOptions.find(item => item.value === supervisorId);
        return new Set((option?.dataset.locationIds ?? '').split(',').filter(Boolean));
    };

    const filterSupervisors = () => {
        const locationId = locationSelect.value;
        const selectedSupervisor = supervisorSelect.value;
        replaceOptions(
            supervisorSelect,
            supervisorOptions,
            option => !locationId || locationsForSupervisor(option.value).has(locationId),
            selectedSupervisor,
            locationId ? 'Seleccionar supervisor de esta sede' : 'Seleccionar'
        );
        if (help) help.textContent = locationId
            ? supervisorSelect.options.length > 1
                ? 'Se muestran únicamente los supervisores asignados a la sede seleccionada.'
                : 'Esta sede todavía no tiene supervisores activos asignados.'
            : 'Elige una sede o un supervisor para limitar las opciones disponibles.';
    };

    const filterLocations = () => {
        const supervisorId = supervisorSelect.value;
        const selectedLocation = locationSelect.value;
        const allowedLocations = locationsForSupervisor(supervisorId);
        replaceOptions(
            locationSelect,
            locationOptions,
            option => !supervisorId || allowedLocations.has(option.value),
            selectedLocation,
            supervisorId ? 'Seleccionar sede asignada' : 'Seleccionar'
        );
        if (supervisorId && !locationSelect.value && allowedLocations.size === 1) {
            locationSelect.value = [...allowedLocations][0];
        }
        if (help) help.textContent = supervisorId
            ? 'Se muestran únicamente las sedes asignadas al supervisor seleccionado.'
            : 'Elige una sede o un supervisor para limitar las opciones disponibles.';
    };

    locationSelect.addEventListener('change', filterSupervisors);
    supervisorSelect.addEventListener('change', filterLocations);
    if (locationSelect.value) filterSupervisors();
    else if (supervisorSelect.value) filterLocations();
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
    const readyMessage = form.querySelector('[data-rubric-ready]');

    const hasStructuredText = field => {
        if (!field) return false;
        const text = field.value.trim();
        return text.length >= 25 && text.split(/\s+/).filter(Boolean).length >= 5;
    };

    const updateCompletionMessage = () => {
        const ratingsComplete = rows.every(row => {
            const rating = row.querySelector('[data-rubric-rating]');
            const requiresDetail = rating?.value === '2' || rating?.value === '3';
            if (!rating || rating.value === '0') return false;
            if (!requiresDetail) return true;
            return [...row.querySelectorAll('[data-rubric-details] textarea')].every(hasStructuredText);
        });
        const generalComplete = overallAssessment?.value !== '0'
            && observedStrength?.value !== '0'
            && hasStructuredText(form.querySelector('[name="OverallEvidence"]'))
            && (overallAssessment?.value !== '3' || hasStructuredText(improvementInput));
        readyMessage.hidden = !(ratingsComplete && generalComplete);
    };

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
        updateCompletionMessage();
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
        updateCompletionMessage();
    };

    rows.forEach(row => {
        row.querySelector('[data-rubric-rating]')?.addEventListener('change', () => updateRubricRow(row));
        updateRubricRow(row);
    });
    overallAssessment?.addEventListener('change', updateOverall);
    observedStrength?.addEventListener('change', updateOverall);
    form.querySelectorAll('textarea').forEach(field => field.addEventListener('input', updateCompletionMessage));
    form.addEventListener('submit', () => {
        rows.forEach(updateRubricRow);
        updateOverall();
    });
    updateOverall();
});

document.querySelectorAll('[data-supervisor-motivator]').forEach(motivator => {
    const toggle = motivator.querySelector('[data-motivator-toggle]');
    const message = motivator.querySelector('[data-motivator-message]');
    const closeButton = motivator.querySelector('[data-motivator-close]');

    const setOpen = open => {
        motivator.classList.toggle('is-open', open);
        message.hidden = !open;
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Ocultar mensaje motivacional' : 'Mostrar mensaje motivacional');
    };

    toggle.addEventListener('click', event => {
        event.stopPropagation();
        setOpen(message.hidden);
    });
    closeButton.addEventListener('click', () => setOpen(false));
    message.addEventListener('click', event => event.stopPropagation());
    document.addEventListener('click', () => setOpen(false));
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') setOpen(false);
    });
});

document.querySelectorAll('[data-collaborator-tour]').forEach(tour => {
    const storageKey = `apparka-collaborator-tour-v1-${tour.dataset.tourUser ?? 'current'}`;
    const steps = [
        { selector: '[data-tour-target="overview"]', title: 'Tu progreso de un vistazo', text: 'Aquí verás tu porcentaje de avance, el puesto y la sede asignada. Puedes volver a abrir esta guía con el botón “Ver guía”.' },
        { selector: '[data-tour-target="resume"]', title: 'Continúa donde te quedaste', text: 'Si una actividad quedó pendiente, este bloque te permitirá retomarla sin empezar nuevamente.' },
        { selector: '[data-tour-target="route"]', title: 'Completa las seis evidencias', text: 'Tu entrenamiento tiene dos actividades por semana. Debes completarlas en orden; cada nueva actividad se habilita cuando corresponde.' },
        { selector: '[data-tour-target="materials"]', title: 'Estudia antes de ejecutar', text: 'Revisa estos módulos para conocer tus funciones, los procedimientos y las recomendaciones de seguridad.' },
        { selector: '[data-tour-target="exam"]', title: 'Prepárate para el examen final', text: 'El examen se habilita después de completar las seis evidencias. Tendrás hasta tres intentos y necesitarás 80% para aprobar.' },
        { selector: '[data-collaborator-nav]', title: 'Navega fácilmente', text: 'Desde la barra inferior puedes volver al inicio o ingresar directamente a tu proceso de entrenamiento.' }
    ].map(step => ({ ...step, target: document.querySelector(step.selector) })).filter(step => step.target);

    const title = tour.querySelector('[data-tour-title]');
    const text = tour.querySelector('[data-tour-text]');
    const counter = tour.querySelector('[data-tour-counter]');
    const previous = tour.querySelector('[data-tour-previous]');
    const next = tour.querySelector('[data-tour-next]');
    const dialog = tour.querySelector('.tour-dialog');
    let current = 0;

    const clearHighlight = () => document.querySelectorAll('.tour-highlight').forEach(element => element.classList.remove('tour-highlight'));
    const close = () => {
        clearHighlight();
        tour.hidden = true;
        document.body.classList.remove('tour-open');
        localStorage.setItem(storageKey, 'completed');
    };
    const finish = () => {
        close();
        requestAnimationFrame(() => window.scrollTo({ top: 0, behavior: 'smooth' }));
    };

    const placeDialog = target => {
        const targetRect = target.getBoundingClientRect();
        const dialogHeight = dialog.offsetHeight;
        const gap = 16;
        const edge = 12;
        const roomBelow = window.innerHeight - targetRect.bottom;
        const roomAbove = targetRect.top;
        let top;

        if (roomBelow >= dialogHeight + gap) {
            top = targetRect.bottom + gap;
            dialog.dataset.placement = 'below';
        } else if (roomAbove >= dialogHeight + gap) {
            top = targetRect.top - dialogHeight - gap;
            dialog.dataset.placement = 'above';
        } else {
            top = Math.max(edge, window.innerHeight - dialogHeight - edge);
            dialog.dataset.placement = 'floating';
        }

        dialog.style.top = `${Math.max(edge, Math.min(top, window.innerHeight - dialogHeight - edge))}px`;
    };

    const render = () => {
        clearHighlight();
        const step = steps[current];
        if (!step) return close();
        const topbarHeight = document.querySelector('.topbar')?.offsetHeight ?? 0;
        const targetTop = step.target.getBoundingClientRect().top + window.scrollY;
        const previousScrollBehavior = document.documentElement.style.scrollBehavior;
        document.documentElement.style.scrollBehavior = 'auto';
        window.scrollTo(0, Math.max(0, targetTop - topbarHeight - 18));
        document.documentElement.style.scrollBehavior = previousScrollBehavior;
        step.target.classList.add('tour-highlight');
        title.textContent = step.title;
        text.textContent = step.text;
        counter.textContent = `PASO ${current + 1} DE ${steps.length}`;
        previous.hidden = current === 0;
        next.textContent = current === steps.length - 1 ? 'Finalizar' : 'Siguiente';
        requestAnimationFrame(() => requestAnimationFrame(() => placeDialog(step.target)));
    };
    const start = () => {
        if (steps.length === 0) return;
        current = 0;
        tour.hidden = false;
        document.body.classList.add('tour-open');
        render();
    };

    document.querySelectorAll('[data-tour-start]').forEach(button => button.addEventListener('click', start));
    tour.querySelectorAll('[data-tour-close]').forEach(button => button.addEventListener('click', close));
    previous.addEventListener('click', () => { if (current > 0) { current--; render(); } });
    next.addEventListener('click', () => { if (current >= steps.length - 1) finish(); else { current++; render(); } });
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && !tour.hidden) close(); });
    window.addEventListener('resize', () => {
        if (!tour.hidden && steps[current]) placeDialog(steps[current].target);
    });

    if (!localStorage.getItem(storageKey)) setTimeout(start, 700);
});

document.querySelectorAll('[data-supervisor-tour]').forEach(tour => {
    const kind = tour.dataset.tourKind;
    const stepSets = {
        dashboard: [
            { selector: '[data-supervisor-tour-target="dashboard-overview"]', title: 'Tu centro de supervisión', text: 'Desde este panel podrás anticipar nuevos ingresos, iniciar actividades y atender únicamente los casos que requieren tu intervención.' },
            { selector: '[data-supervisor-tour-target="dashboard-metrics"]', title: 'Prioriza tu jornada', text: 'Estos indicadores resumen cuántos colaboradores están activos, qué prácticas debes validar y qué actividades están listas para comenzar.' },
            { selector: '[data-supervisor-tour-target="dashboard-pending"]', title: 'Valida la práctica observada', text: 'Aquí aparecen los colaboradores que ya respondieron sus preguntas. Tu tarea es evaluar cómo ejecutaron la actividad durante sus funciones.' },
            { selector: '[data-supervisor-tour-target="dashboard-codes"]', title: 'Genera un acceso de un solo uso', text: 'El código temporal permite que el colaborador inicie únicamente la actividad disponible. No necesitas prestarle tu teléfono ni realizarle el cuestionario.' },
            { selector: '[data-supervisor-tour-target="dashboard-periods"]', title: 'Anticipa y consulta tus ingresos', text: 'Revisa quiénes están por comenzar, su sede y su avance. Los filtros te ayudarán cuando tengas varios colaboradores asignados.' },
            { selector: '[data-supervisor-nav]', title: 'Vuelve rápidamente a supervisión', text: 'La barra inferior te permite regresar al panel desde cualquier sección disponible para tu rol.' }
        ],
        review: [
            { selector: '[data-supervisor-tour-target="review-challenge"]', title: 'Evalúa la ejecución, no el cuestionario', text: 'El colaborador ya respondió la parte teórica. Observa cómo realiza el reto práctico durante sus funciones y califica únicamente lo que realmente viste.' },
            { selector: '[data-supervisor-tour-target="review-rubric"]', title: 'Completa los cinco criterios', text: 'Selecciona el nivel alcanzado en cada criterio. Si marcas cumplimiento parcial o incumplimiento, describe el hecho observado y la orientación que brindaste.' },
            { selector: '[data-supervisor-tour-target="review-overall"]', title: 'Registra una conclusión verificable', text: 'Indica el resultado general, el principal aspecto correcto y una evidencia concreta. Si requiere mejora, especifica también qué debe reforzar.' },
            { selector: '[data-supervisor-tour-target="review-motivator"]', title: 'Tu observación sí importa', text: 'Este asistente te recordará que una descripción clara ayuda al colaborador a mejorar y protege la calidad del entrenamiento.' },
            { selector: '[data-supervisor-tour-target="review-close"]', title: 'Revisa antes de cerrar', text: 'Al cerrar se registrarán la rúbrica, las observaciones y el porcentaje final. Esta evidencia no tendrá un nuevo intento.' }
        ]
    };
    const steps = (stepSets[kind] ?? [])
        .map(step => ({ ...step, target: document.querySelector(step.selector) }))
        .filter(step => step.target);

    const title = tour.querySelector('[data-supervisor-tour-title]');
    const text = tour.querySelector('[data-supervisor-tour-text]');
    const counter = tour.querySelector('[data-supervisor-tour-counter]');
    const previous = tour.querySelector('[data-supervisor-tour-previous]');
    const next = tour.querySelector('[data-supervisor-tour-next]');
    const dialog = tour.querySelector('.tour-dialog');
    let current = 0;
    let completionSent = tour.dataset.tourCompleted === 'true';

    const clearHighlight = () => document.querySelectorAll('.tour-highlight').forEach(element => element.classList.remove('tour-highlight'));
    const saveCompletion = () => {
        if (completionSent) return;
        completionSent = true;
        tour.dataset.tourCompleted = 'true';
        const token = tour.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const url = tour.dataset.tourCompleteUrl;
        if (!token || !url) return;
        const body = new URLSearchParams({ guide: kind, __RequestVerificationToken: token });
        fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
            body: body.toString(),
            keepalive: true
        }).catch(() => { completionSent = false; });
    };
    const close = () => {
        clearHighlight();
        tour.hidden = true;
        document.body.classList.remove('tour-open');
        saveCompletion();
    };
    const finish = () => {
        close();
        requestAnimationFrame(() => window.scrollTo({ top: 0, behavior: 'smooth' }));
    };
    const placeDialog = target => {
        const targetRect = target.getBoundingClientRect();
        const dialogHeight = dialog.offsetHeight;
        const gap = 16;
        const edge = 12;
        const roomBelow = window.innerHeight - targetRect.bottom;
        const roomAbove = targetRect.top;
        let top;

        if (roomBelow >= dialogHeight + gap) {
            top = targetRect.bottom + gap;
            dialog.dataset.placement = 'below';
        } else if (roomAbove >= dialogHeight + gap) {
            top = targetRect.top - dialogHeight - gap;
            dialog.dataset.placement = 'above';
        } else {
            top = Math.max(edge, window.innerHeight - dialogHeight - edge);
            dialog.dataset.placement = 'floating';
        }

        dialog.style.top = `${Math.max(edge, Math.min(top, window.innerHeight - dialogHeight - edge))}px`;
    };
    const render = () => {
        clearHighlight();
        const step = steps[current];
        if (!step) return close();
        const topbarHeight = document.querySelector('.topbar')?.offsetHeight ?? 0;
        const targetTop = step.target.getBoundingClientRect().top + window.scrollY;
        const previousScrollBehavior = document.documentElement.style.scrollBehavior;
        document.documentElement.style.scrollBehavior = 'auto';
        window.scrollTo(0, Math.max(0, targetTop - topbarHeight - 18));
        document.documentElement.style.scrollBehavior = previousScrollBehavior;
        step.target.classList.add('tour-highlight');
        title.textContent = step.title;
        text.textContent = step.text;
        counter.textContent = `PASO ${current + 1} DE ${steps.length}`;
        previous.hidden = current === 0;
        next.textContent = current === steps.length - 1 ? 'Finalizar' : 'Siguiente';
        requestAnimationFrame(() => requestAnimationFrame(() => placeDialog(step.target)));
    };
    const start = () => {
        if (steps.length === 0) return;
        current = 0;
        tour.hidden = false;
        document.body.classList.add('tour-open');
        render();
    };

    document.querySelectorAll(`[data-supervisor-tour-start="${kind}"]`).forEach(button => button.addEventListener('click', start));
    tour.querySelectorAll('[data-supervisor-tour-close]').forEach(button => button.addEventListener('click', close));
    previous.addEventListener('click', () => { if (current > 0) { current--; render(); } });
    next.addEventListener('click', () => { if (current >= steps.length - 1) finish(); else { current++; render(); } });
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && !tour.hidden) close(); });
    window.addEventListener('resize', () => { if (!tour.hidden && steps[current]) placeDialog(steps[current].target); });

    if (!completionSent) setTimeout(start, 700);
});

document.querySelectorAll('[data-exception-dialog]').forEach(dialog => {
    const form = dialog.querySelector('[data-exception-form]');
    const title = dialog.querySelector('[data-exception-title]');
    const evidenceId = dialog.querySelector('[data-exception-evidence-id]');
    const dueAt = dialog.querySelector('input[name="NewDueAt"]');
    const reasonGroup = dialog.querySelector('[data-exception-reason]');
    const reason = reasonGroup.querySelector('textarea');
    const modeInputs = dialog.querySelectorAll('[data-exception-mode]');

    const suggestedDueAt = () => {
        const value = new Date(Date.now() + 24 * 60 * 60 * 1000);
        value.setMinutes(value.getMinutes() - value.getTimezoneOffset());
        return value.toISOString().slice(0, 16);
    };
    const updateMode = () => {
        const selected = dialog.querySelector('[data-exception-mode]:checked');
        const requiresReason = selected?.value === 'false';
        reasonGroup.hidden = !requiresReason;
        reason.required = requiresReason;
        if (!requiresReason) reason.value = '';
    };
    const close = () => dialog.close();

    document.querySelectorAll('[data-exception-open]').forEach(button => {
        button.addEventListener('click', () => {
            form.reset();
            form.action = button.dataset.exceptionAction;
            title.textContent = button.dataset.exceptionTitle || 'Activación excepcional';
            evidenceId.value = button.dataset.exceptionKind === 'activity'
                ? button.dataset.exceptionId
                : '';
            dueAt.value = suggestedDueAt();
            updateMode();
            dialog.showModal();
        });
    });
    modeInputs.forEach(input => input.addEventListener('change', updateMode));
    dialog.querySelectorAll('[data-exception-close]').forEach(button => button.addEventListener('click', close));
    dialog.addEventListener('click', event => {
        if (event.target === dialog) close();
    });
});
