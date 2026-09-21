// GIVE-AID Main Interactive Script

document.addEventListener('DOMContentLoaded', () => {
    // Check if user prefers reduced motion
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // 1. GSAP Hero Entrance Animations
    if (!prefersReducedMotion && typeof gsap !== 'undefined') {
        gsap.from('.gsap-hero-title', {
            opacity: 0,
            y: 30,
            duration: 0.8,
            ease: 'power2.out'
        });

        gsap.from('.gsap-hero-subtitle', {
            opacity: 0,
            y: 20,
            duration: 0.8,
            delay: 0.2,
            ease: 'power2.out'
        });

        gsap.from('.gsap-hero-actions', {
            opacity: 0,
            y: 20,
            duration: 0.8,
            delay: 0.4,
            ease: 'power2.out'
        });

        // 2. Animated Number Counters
        const statCounters = document.querySelectorAll('.counter-val');
        if (statCounters.length > 0) {
            statCounters.forEach(counter => {
                const target = parseFloat(counter.getAttribute('data-target') || '0');
                const isCurrency = counter.getAttribute('data-currency') === 'true';
                
                gsap.to(counter, {
                    innerHTML: target,
                    duration: 1.5,
                    ease: 'power1.out',
                    snap: { innerHTML: 1 },
                    onUpdate: function () {
                        const val = Math.floor(counter.innerHTML);
                        if (isCurrency) {
                            counter.innerHTML = '$' + val.toLocaleString();
                        } else {
                            counter.innerHTML = val.toLocaleString();
                        }
                    }
                });
            });
        }
    }

    // 3. AJAX Programme Interest Handler
    const interestForms = document.querySelectorAll('.ajax-interest-form');
    interestForms.forEach(form => {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const btn = form.querySelector('button[type="submit"]');
            const originalText = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Registering...';

            try {
                const formData = new FormData(form);
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (response.ok) {
                    const data = await response.json();
                    if (data.alreadyRegistered) {
                        btn.className = 'btn btn-outline-secondary w-100 disabled';
                        btn.innerHTML = '<i class="bi bi-check2-circle me-1"></i> Already Registered';
                        showToast(data.message, 'info');
                    } else if (data.success) {
                        btn.className = 'btn btn-success w-100 disabled';
                        btn.innerHTML = '<i class="bi bi-check-circle-fill me-1"></i> Registered!';
                        showToast(data.message, 'success');
                    } else {
                        btn.disabled = false;
                        btn.innerHTML = originalText;
                        showToast(data.message, 'error');
                    }
                } else if (response.status === 401) {
                    window.location.href = '/account/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                } else {
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                    showToast('Failed to register interest. Please try again.', 'error');
                }
            } catch (err) {
                console.error(err);
                btn.disabled = false;
                btn.innerHTML = originalText;
                showToast('Network error while registering interest.', 'error');
            }
        });
    });

    // 4. Client Notification Helper
    function showToast(message, type = 'info') {
        const alertBox = document.createElement('div');
        const alertClass = type === 'success' ? 'alert-success' : type === 'error' ? 'alert-danger' : 'alert-info';
        alertBox.className = `alert ${alertClass} alert-dismissible fade show position-fixed top-0 end-0 m-3 shadow-lg z-3`;
        alertBox.style.minWidth = '300px';
        alertBox.innerHTML = `
            <strong>${type === 'success' ? 'Success' : type === 'error' ? 'Notice' : 'Information'}:</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;
        document.body.appendChild(alertBox);
        setTimeout(() => {
            alertBox.classList.remove('show');
            setTimeout(() => alertBox.remove(), 300);
        }, 5000);
    }
});
