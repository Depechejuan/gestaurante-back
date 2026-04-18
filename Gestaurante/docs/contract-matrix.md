# Contract Matrix: API, Database and Frontend Routes

This document is the working matrix used to keep the mounted frontend routes in `gestaurante-front/src/App.jsx` aligned with the real API controllers in `Gestaurante/Controllers` and with the persisted aggregates declared in `Gestaurante/Models/Data/AppDbContext.cs`.

## Persistent aggregates

| Aggregate | DbSet / table | Main relations |
| --- | --- | --- |
| Employees | `Empleados` | Discriminator by `Tipo`; auth for `/user` and `/admin/*` |
| Customers | `UsuariosCliente` | Owns `ClienteDirecciones`, `ClienteMetodosPago`, `ClienteEmailVerifications` |
| Catalog categories | `Categorias` | One-to-many with `Platos` |
| Ingredients | `Ingredientes` | Many-to-many with `Platos` through `PlatoIngrediente` |
| Dishes | `Platos` | Belongs to `Categoria`; joins `Ingrediente` |
| Tables | `Mesas` | One-to-many logical link with `Pedidos` |
| Public table sessions | `MesaPublicSessions` | Belongs to `Mesa`; authorizes QR flow via `X-Mesa-Session` |
| Orders | `Pedidos` | May belong to `Mesa`, `MesaPublicSession`, `UsuarioCliente`, `Factura` |
| Order lines | `DetallesPedido` | Belongs to `Pedido`; points to `Plato` |
| Invoices | `Facturas` | May point to `Mesa`, `Pedido`, `UsuarioCliente` |

## Backend controller matrix

| Controller | Auth / header convention | Aggregates touched | Routes |
| --- | --- | --- | --- |
| `UserController` | Employee JWT | `Empleados` | `POST /user/login`, `GET /user/me` |
| `AdminController` | Employee JWT, role `Administrador` | `Empleados` | `POST /admin/register`, `POST /admin/getbasicuser`, `POST /admin/getusers`, `GET /admin/user/{id}`, `PUT /admin/user/{id}`, `PUT /admin/user/{id}/photo` |
| `ClienteController` | Employee JWT, roles `Administrador,Camarero` | `UsuariosCliente` | `GET /Cliente`, `GET /Cliente/{id}`, `POST /Cliente`, `PUT /Cliente/{id}`, `PATCH /Cliente/{id}/estado` |
| `FacturaController` | Employee JWT, roles `Administrador,Camarero` | `Facturas`, `Pedidos`, `UsuariosCliente` | `GET /Factura`, `GET /Factura/{id}`, `GET /Factura/clientes/search`, `POST /Factura`, `PUT /Factura/{id}`, `PUT /Factura/{id}/cliente`, `POST /Factura/{id}/cobrar`, `POST /Factura/{id}/send-email`, `DELETE /Factura/{id}` |
| `MesaController` | Employee JWT, roles `Administrador,Camarero` | `Mesas`, `Pedidos`, `Facturas` | `GET /Mesa`, `GET /Mesa/{id}`, `POST /Mesa`, `PUT /Mesa/{id}`, `DELETE /Mesa/{id}`, `POST /Mesa/{id}/cerrar` |
| `PedidoController` | Employee JWT, roles `Administrador,Camarero,Cocinero,Repartidor` | `Pedidos`, `DetallesPedido`, `Mesas`, `Facturas` | `GET /Pedido`, `GET /Pedido/{id}`, `POST /Pedido`, `PUT /Pedido/{id}`, `DELETE /Pedido/{id}`, `POST /Pedido/{id}/cancelar`, `GET /Pedido/{pedidoId}/linea/{detalleId}`, `POST /Pedido/{pedidoId}/linea`, `PUT /Pedido/{pedidoId}/linea/{detalleId}`, `DELETE /Pedido/{pedidoId}/linea/{detalleId}`, `POST /Pedido/{pedidoId}/linea/{detalleId}/cancelar` |
| `PlatoController` | Read public, writes admin JWT | `Platos`, `PlatoIngrediente`, `Categorias`, `Ingredientes` | `GET /Plato`, `GET /Plato/{id}`, `POST /Plato`, `PUT /Plato/{id}`, `DELETE /Plato/{id}`, `PATCH /Plato/{id}/disponibilidad` |
| `CategoriaController` | Read public, writes admin JWT | `Categorias` | `GET /Categoria`, `GET /Categoria/{id}`, `POST /Categoria`, `PUT /Categoria/{id}`, `DELETE /Categoria/{id}` |
| `IngredienteController` | Read public, writes admin JWT | `Ingredientes` | `GET /Ingrediente`, `GET /Ingrediente/{id}`, `POST /Ingrediente`, `PUT /Ingrediente/{id}`, `DELETE /Ingrediente/{id}` |
| `PublicCatalogController` | Public | `Platos`, `Categorias`, `Ingredientes` | `GET /public/catalogo`, `GET /public/catalogo/{id}` |
| `PublicAccountController` | Public or customer JWT | `UsuariosCliente`, `ClienteEmailVerifications`, `ClienteDirecciones`, `ClienteMetodosPago` | `POST /public/account/register`, `POST /public/account/verify-email`, `POST /public/account/resend-code`, `POST /public/account/login`, `GET /public/account/me`, `PUT /public/account/profile`, `GET /public/account/addresses`, `POST /public/account/addresses`, `PUT /public/account/addresses/{id}`, `DELETE /public/account/addresses/{id}`, `GET /public/account/payment-methods`, `POST /public/account/payment-methods`, `DELETE /public/account/payment-methods/{id}` |
| `PublicCheckoutController` | Customer JWT | `Pedidos`, `Facturas`, `UsuariosCliente`, `ClienteDirecciones`, `ClienteMetodosPago` | `POST /public/checkout/order`, `GET /public/account/orders`, `GET /public/account/orders/{id}` |
| `PublicMesaController` | Public, header `X-Mesa-Session` after session open | `MesaPublicSessions`, `Mesas`, `Pedidos`, `DetallesPedido` | `POST /public/mesa/{id}/session`, `GET /public/mesa/{id}/pedidos`, `POST /public/mesa/{id}/pedido` |

## Frontend route matrix

Legacy wrappers such as `src/services/get-basic-user.js`, `get-empleado.js`, `get-empleados.js` and `get-platos.js` are now adapters over the canonical modules and should not grow new contract logic.

### Public and customer routes

| Route | Page / component | Canonical service layer | Primary API contract | Auth / header |
| --- | --- | --- | --- | --- |
| `/` | `Pages/Home.jsx` | none | none | public |
| `/login` | `Components/Forms/Login.jsx` | `services/login.js`, `services/customer-account.js` | `POST /user/login`, `POST /public/account/login` | public |
| `/carta` | `Pages/PlatosPublic.jsx` + `Hooks/usePlatos.js` | `services/public-catalog.js` | `GET /public/catalogo` | public |
| `/carta/:id` | `Pages/UniquePlatoPublic.jsx` | `services/public-catalog.js` | `GET /public/catalogo/{id}` | public |
| `/mesa/:id` | `Pages/MesaQrMenu.jsx` | `services/public-mesa.js`, `services/public-catalog.js` | `POST /public/mesa/{id}/session`, `GET /public/catalogo`, `GET /public/mesa/{id}/pedidos`, `POST /public/mesa/{id}/pedido` | public, then `X-Mesa-Session` |
| `/pedido-online` | `Pages/OnlineOrder.jsx` | `services/public-catalog.js`, `services/customer-account.js`, `services/online-order.js` | `GET /public/catalogo`, optional `GET /public/account/addresses`, `GET /public/account/payment-methods`, `POST /public/checkout/order` | public or customer JWT |
| `/checkout` | `Pages/OnlineOrder.jsx` | same as `/pedido-online` | same as `/pedido-online` | customer JWT for checkout submit |
| `/cuenta/register` | `Pages/CustomerRegister.jsx` | `services/customer-account.js` | `POST /public/account/register` | public |
| `/cuenta/verificar-email` | `Pages/CustomerVerifyEmail.jsx` | `services/customer-account.js` | `POST /public/account/verify-email`, `POST /public/account/resend-code` | public |
| `/cuenta/login` | redirect to `/login` | shared login form | shared login endpoints | public |
| `/cuenta` | `Pages/CustomerAccount.jsx` | `services/customer-account.js` | `PUT /public/account/profile`, `GET /public/account/orders` | customer JWT |
| `/cuenta/pedidos` | `Pages/CustomerOrders.jsx` | `services/customer-account.js`, `services/public-catalog.js` | `GET /public/account/orders`, `GET /public/catalogo` | customer JWT for orders |
| `/cuenta/direcciones` | `Pages/CustomerAddresses.jsx` | `services/customer-account.js` | `GET/POST/PUT/DELETE /public/account/addresses` | customer JWT |
| `/cuenta/metodos-pago` | `Pages/CustomerPaymentMethods.jsx` | `services/customer-account.js` | `GET/POST/DELETE /public/account/payment-methods` | customer JWT |
| `/about`, `/contacto` | static pages | none | none | public |

### Staff and admin routes

| Route | Page / component | Canonical service layer | Primary API contract | Auth / header |
| --- | --- | --- | --- | --- |
| `/staff` | `Pages/Dashboard-Staff.jsx` | contextual hooks only | none | employee JWT |
| `/staff/mesas` | `Pages/Mesas.jsx` | `services/mesas.js` | `GET/POST/PUT/DELETE /Mesa` | employee JWT |
| `/staff/mesas/:id` | `Pages/MesaDetail.jsx` | `services/mesas.js` | `GET /Mesa/{id}`, `POST /Mesa/{id}/cerrar` | employee JWT |
| `/staff/pedidos` | `Pages/Pedidos.jsx` | `services/pedidos.js` | `GET /Pedido` | employee JWT |
| `/staff/pedidos/:id` | `Pages/UniquePedido.jsx` | `services/pedidos.js`, `services/facturas.js` | `GET /Pedido/{id}`, `PUT /Pedido/{id}`, `POST /Pedido/{id}/cancelar`, `PUT /Pedido/{pedidoId}/linea/{detalleId}`, `POST /Pedido/{pedidoId}/linea/{detalleId}/cancelar`, `POST /Factura` | employee JWT |
| `/staff/online` | `Pages/PedidosOnline.jsx` | `services/pedidos.js` | `GET /Pedido` filtered to online | employee JWT |
| `/staff/entregas`, `/staff/reparto` | redirects to `/staff/online?view=...` | same as `/staff/online` | same as `/staff/online` | employee JWT |
| `/staff/facturas` | `Pages/Facturas.jsx` | `services/facturas.js` | `GET /Factura` | employee JWT |
| `/staff/facturas/:id` | `Pages/UniqueFactura.jsx` | `services/facturas.js` | `GET /Factura/{id}`, `GET /Factura/clientes/search`, `PUT /Factura/{id}`, `PUT /Factura/{id}/cliente`, `POST /Factura/{id}/cobrar`, `POST /Factura/{id}/send-email` | employee JWT |
| `/staff/clientes` | `Pages/Clientes.jsx` | `services/clientes.js` | `GET /Cliente` | employee JWT |
| `/staff/clientes/:id` | `Pages/UniqueCliente.jsx` | `services/clientes.js` | `GET /Cliente/{id}` | employee JWT |
| `/dashboard` | `Pages/Dashboard.jsx` | contextual hooks only | none | admin JWT |
| `/dashboard/register` | `Components/Forms/Register.jsx` | `services/register.js` | `POST /admin/register` | admin JWT |
| `/dashboard/empleados` | `Components/Empleados.jsx` | `services/empleados.js` | `POST /admin/getusers` | admin JWT |
| `/dashboard/empleados/:id` | `Components/UniqueEmpleado.jsx`, `Components/Forms/Edit-User.jsx` | `services/empleados.js` | `GET /admin/user/{id}`, `PUT /admin/user/{id}` | admin JWT, `multipart/form-data` on update |
| `/dashboard/facturas`, `/dashboard/facturas/:id` | same components as staff | `services/facturas.js` | same `FacturaController` contract | admin JWT |
| `/dashboard/clientes`, `/dashboard/clientes/:id` | same components as staff with edit actions | `services/clientes.js` | `GET /Cliente`, `GET /Cliente/{id}`, `PUT /Cliente/{id}`, `PATCH /Cliente/{id}/estado` | admin JWT |
| `/dashboard/mesas`, `/dashboard/mesas/:id` | same components as staff with create/edit/delete actions | `services/mesas.js` | `GET/POST/PUT/DELETE /Mesa`, `POST /Mesa/{id}/cerrar` | admin JWT |
| `/dashboard/carta` | `Pages/PlatosAdmin.jsx` | `services/platos.js`, `services/categorias.js`, `services/ingredientes.js` | `GET/POST/PATCH /Plato`, `GET/POST /Categoria`, `GET/POST /Ingrediente` | admin JWT |
| `/dashboard/plato/:id` | `Pages/UniquePlatoAdmin.jsx` | `services/platos.js`, `services/categorias.js`, `services/ingredientes.js` | `GET /Plato/{id}`, `PUT /Plato/{id}`, support lookups in `Categoria` and `Ingrediente` | admin JWT |

## Test coverage now living in the repos

### Backend integration

- `Gestaurante.ApiTests/EmployeeEndpointsTests.cs`
- `Gestaurante.ApiTests/CatalogEndpointsTests.cs`
- `Gestaurante.ApiTests/CustomerEndpointsTests.cs`
- `Gestaurante.ApiTests/BillingEndpointsTests.cs`
- `Gestaurante.ApiTests/OperationsEndpointsTests.cs`
- `Gestaurante.ApiTests/PublicOrderingEndpointsTests.cs`

These tests run against a dedicated PostgreSQL database `requier_test`, not an in-memory provider, and validate auth, CRUD, business transitions, QR flow, customer checkout, invoices and employee edition.

### Frontend smoke and integration

- `src/App.routes.test.jsx`
- `src/Pages/Facturas.test.jsx`
- `src/Components/Forms/Edit-User.test.jsx`
- `src/Hooks/usePlatos.test.jsx`
- `src/services/public-catalog.test.js`

The frontend suite runs with Vitest + React Testing Library and uses MSW-backed handlers for networked views, covering mounted routes, invoice listing, the employee edit form and the public catalog flow that feeds `/carta` and `/pedido-online`.
