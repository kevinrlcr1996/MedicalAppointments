export const apiBaseUrl = (
  import.meta.env.VITE_API_BASE_URL ?? ''
).replace(/\/$/, '');

export const foundationHubUrl =
  import.meta.env.VITE_FOUNDATION_HUB_URL ??
  `${apiBaseUrl}/hubs/foundation`;

export const notificationHubUrl =
  import.meta.env.VITE_NOTIFICATION_HUB_URL ??
  `${apiBaseUrl}/hubs/notifications`;
