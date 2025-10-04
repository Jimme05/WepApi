# SimpleAuthBasicApi (No token, No cookie) — HTTP Basic Auth

- ใช้ **HTTP Basic Auth**: Client ส่ง `Authorization: Basic base64(email:password)` ทุกครั้ง
- ไม่มีการเก็บ token หรือ cookie ใด ๆ (Stateless)
- ฐานข้อมูล MySQL ตารางเดียว `users` (เช่นเดียวกับ SimpleAuthApi)

## DB schema (phpMyAdmin)
```sql
CREATE DATABASE IF NOT EXISTS simpleauth
  DEFAULT CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE simpleauth;
CREATE TABLE IF NOT EXISTS users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  display_name VARCHAR(100) NULL,
  avatar_url VARCHAR(255) NULL,
  role ENUM('User','Admin') NOT NULL DEFAULT 'User',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Run
```
dotnet restore
dotnet run
```
Swagger: `http://localhost:6060/swagger`

## ใช้งานใน Swagger
- กด **Authorize** เลือก **basic** ใส่ `email:password`
- เรียก `/api/users/me` หรือ `/api/users` (ต้อง role=Admin)

## Angular
```ts
const cred = btoa(`${email}:${password}`);
this.http.get('/api/users/me', {
  headers: { Authorization: `Basic ${cred}` }
});
```
> ควรใช้ HTTPS เสมอ เพราะ Basic จะส่งรหัสผ่านในรูป base64 (เข้ารหัสช่องทางด้วย TLS)
