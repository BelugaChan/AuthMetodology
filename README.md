## AuthMetodology 🔐 

Мини-проект по разработке системы аутентификации и авторизации пользователей с использованием ASP.NET Core.

---

### Описание проекта 📌

AuthMetodology — это backend-сервис, реализующий базовую и расширенную логику аутентификации пользователей:

* регистрация и вход в систему
* работа с JWT (access + refresh токены)
* авторизация через Google
* восстановление и сброс пароля
* логирование через очередь сообщений (RabbitMQ)

---

### Стек технологий ⚙️

* .NET / ASP.NET Core
* C#
* JWT (JSON Web Tokens)
* RabbitMQ
* xUnit
* Moq
* Google Authentication API
* REST API

---

### Архитектура проекта 🧱

Проект построен по принципам многослойной архитектуры:

AuthMetodology<br>
│<br>
├── API                # Контроллеры (входные точки)<br>
├── Application        # DTO, интерфейсы, бизнес-логика<br>
├── Infrastructure     # Модели, внешние сервисы<br>
├── xUnitTests         # Unit-тесты

---

### Основной функционал API
🔑 Аутентификация
* POST /api/v1/auth/register — регистрация пользователя
* POST /api/v1/auth/login — вход в систему
* POST /api/v1/auth/refresh — обновление токенов
  
🔐 Работа с паролем

* POST /api/v1/auth/forgot-password — запрос на восстановление
* POST /api/v1/auth/reset-password — сброс пароля

🌐 Google авторизация

* POST /api/v1/auth/googleLogin
* POST /api/v1/auth/googleRegister

---

### Аутентификация 🔒

Используется схема:

* Access Token — короткоживущий
* Refresh Token — долгоживущий

Токены сохраняются в HTTP-only cookies.

---

### Тестирование 🧪

В проекте реализованы unit-тесты с использованием:

* xUnit
* Moq

Покрываются:

* успешные сценарии (login, register)
* обработка ошибок (ModelState)
* проверка вызова сервисов
* работа с cookie
* логирование

Пример тестируемого поведения:

* корректность ответа API
* создание cookies
* отправка логов в очередь

---

### Как запустить проект 🛠️

1. Клонировать репозиторий
``` git clone https://github.com/BelugaChan/AuthMetodology.git ```
2. Перейти в папку проекта
``` cd AuthMetodology ```
3. Настроить конфигурацию


Указать в appsettings.json:

* JWT SecretKey
* настройки RabbitMQ
* Google Auth (если используется)
4. Запустить проект
``` dotnet run ```

---

### Особенности реализации
* Использование Dependency Injection
* Разделение ответственности через интерфейсы
* Асинхронная работа с логированием
* Проверка ModelState во всех критичных точках
* Безопасная работа с токенами
