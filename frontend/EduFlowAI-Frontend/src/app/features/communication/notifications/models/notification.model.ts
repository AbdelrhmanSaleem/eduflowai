export const NOTIFICATION_TYPES = [
  'DocumentsRequired',
  'DocumentReplacementRequested',
  'DocumentApproved',
  'DocumentRejected',
  'ApplicationStatusChanged',
] as const;

export type NotificationType = 'None' | (typeof NOTIFICATION_TYPES)[number];

export interface NotificationItem {
  id: string;
  message: string | null;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
}

export interface PaginatedResult<T> {
  data: T[];
  filters: unknown | null;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
}

export interface NotificationQuery {
  page: number;
  pageSize: number;
  search?: string;
  type?: Exclude<NotificationType, 'None'>;
}

export interface MarkAllReadResponse {
  updatedCount: number;
}

export interface NotificationCreatedMessage {
  notificationId: string;
  applicationId: string | null;
  notificationType: NotificationType | number | string;
  message: string | null;
  createdAtUtc: string;
}
