﻿# API и события (минимум)

## Orders Service
- `POST /orders` — создать заказ. Body: `{ "userId": "string", "amount": decimal }`.
- `GET /orders?userId=...` — список заказов пользователя.
- `GET /orders/{id}` — статус заказа.

### События
- `OrderCreated { orderId: guid, userId: string, amount: decimal }`
- `PaymentStatusChanged { orderId: guid, status: "Paid"|"Failed" }`

## Payments Service
- `POST /accounts` — создать счет (1 на пользователя). Body: `{ "userId": "string" }`.
- `POST /accounts/top-up` — пополнить счет. Body: `{ "userId": "string", "amount": decimal }`.
- `GET /accounts?userId=...` — получить счет.

### Подписки/публикации
- Consume: `OrderCreated`
- Publish: `PaymentStatusChanged`

## Swagger через gateway
- Orders Swagger: http://localhost:8080/swagger/orders/index.html
- Payments Swagger: http://localhost:8080/swagger/payments/index.html

