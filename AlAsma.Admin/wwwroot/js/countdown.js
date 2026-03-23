document.addEventListener('DOMContentLoaded', function () {
    const countdowns = document.querySelectorAll('.countdown');

    countdowns.forEach(function (el) {
        const deadlineAttr = el.getAttribute('data-deadline');
        if (!deadlineAttr) return;
        
        const deadline = new Date(deadlineAttr);

        function update() {
            const now = new Date();
            const diff = deadline - now;

            if (diff <= 0) {
                el.textContent = 'منتهي';
                el.classList.add('text-danger');
                clearInterval(timer);
                return;
            }

            const days = Math.floor(diff / (1000 * 60 * 60 * 24));
            const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((diff % (1000 * 60)) / 1000);

            el.textContent = `${days}ي ${hours}س ${minutes}د ${seconds}ث`;
        }

        update();
        const timer = setInterval(update, 1000);
    });
});
