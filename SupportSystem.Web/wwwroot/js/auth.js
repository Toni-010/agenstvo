// Константы
const API_BASE_URL = 'http://localhost:5000'; // или ваш порт API
const STORAGE_TOKEN_KEY = 'support_system_token';
const STORAGE_USER_KEY = 'support_system_user';

// Функции для работы с localStorage
export const authService = {
    // Сохранить токен и данные пользователя
    saveAuthData(token, user) {
        localStorage.setItem(STORAGE_TOKEN_KEY, token);
        localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(user));
    },

    // Получить токен
    getToken() {
        return localStorage.getItem(STORAGE_TOKEN_KEY);
    },

    // Получить данные пользователя
    getUser() {
        const userJson = localStorage.getItem(STORAGE_USER_KEY);
        return userJson ? JSON.parse(userJson) : null;
    },

    // Проверить авторизацию
    isAuthenticated() {
        return !!this.getToken();
    },

    // Проверить роль
    hasRole(role) {
        const user = this.getUser();
        return user && user.role === role;
    },

    // Получить заголовки авторизации
    getAuthHeaders() {
        const token = this.getToken();
        return token ? { 'Authorization': `Bearer ${token}` } : {};
    },

    // Выход
    logout() {
        localStorage.removeItem(STORAGE_TOKEN_KEY);
        localStorage.removeItem(STORAGE_USER_KEY);
        window.location.href = '/login.html';
    }
};

// Функция показа уведомления
export function showNotification(message, type = 'info') {
    // Удаляем старое уведомление если есть
    let notification = document.getElementById('notification');
    if (!notification) {
        notification = document.createElement('div');
        notification.id = 'notification';
        document.body.appendChild(notification);
    }

    notification.textContent = message;
    notification.className = `notification ${type}`;
    notification.style.display = 'block';

    // Автоскрытие через 3 секунды
    setTimeout(() => {
        notification.style.display = 'none';
    }, 3000);
}

// Функция перенаправления по роли
export function redirectByRole(role) {
    switch (role) {
        case 'Admin':
            window.location.href = '/admin-dashboard.html';
            break;
        case 'Manager':
            window.location.href = '/manager-dashboard.html';
            break;
        case 'User':
            window.location.href = '/user-dashboard.html';
            break;
        default:
            window.location.href = '/login.html';
    }
}

// API функции
export const api = {
    // Логин
    async login(email, password) {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ email, password })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Ошибка входа');
            }

            return await response.json();
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    },

    // Проверить авторизацию
    async checkAuth() {
        try {
            const token = authService.getToken();
            if (!token) return false;

            const response = await fetch(`${API_BASE_URL}/api/Users`, {
                headers: authService.getAuthHeaders()
            });

            return response.ok;
        } catch (error) {
            console.error('Auth check error:', error);
            return false;
        }
    }
};

// Инициализация страницы с проверкой авторизации
export function initAuthPage() {
    const token = authService.getToken();
    const user = authService.getUser();

    if (token && user) {
        // Если уже авторизован, редиректим на дашборд
        redirectByRole(user.role);
    }
}

// Защита страницы по роли
export function protectPage(allowedRoles = []) {
    const user = authService.getUser();

    if (!authService.isAuthenticated()) {
        // Не авторизован - на логин
        window.location.href = '/login.html';
        return false;
    }

    if (allowedRoles.length > 0 && !allowedRoles.includes(user.role)) {
        // Нет прав - на главную
        window.location.href = '/';
        return false;
    }

    return true;
}