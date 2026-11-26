// Configuración de producción. El frontend y la API se publican bajo el mismo dominio,
// así que la API se llama con una ruta relativa.
export const environment = {
  production: true,
  apiUrl: '/api',
};
