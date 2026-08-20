import type { AppLocale } from '../../../../../core/i18n/locale.store';
import type { NotificationType } from '../../models/notification.model';

interface NotificationCenterCopy {
  eyebrow: string;
  title: string;
  subtitle: string;
  count: string;
  search: string;
  filter: string;
  allTypes: string;
  sort: string;
  newest: string;
  oldest: string;
  loading: string;
  loadingCopy: string;
  errorTitle: string;
  errorCopy: string;
  emptyTitle: string;
  emptyCopy: string;
  list: string;
  inbox: string;
  of: string;
  noMessage: string;
  unread: string;
  read: string;
  sent: string;
  message: string;
  pagination: string;
  previous: string;
  next: string;
  markAll: string;
  markingAll: string;
  readUpdateError: string;
  backToInbox: string;
  types: Record<NotificationType, string>;
  actions: {
    documents: string;
    replacements: string;
    applications: string;
    support: string;
  };
}

export const NOTIFICATION_CENTER_COPY: Record<AppLocale, NotificationCenterCopy> = {
  en: {
    eyebrow: 'Applicant inbox',
    title: 'Notifications',
    subtitle: 'Review official correspondence and application updates.',
    count: 'notifications',
    search: 'Search notifications…',
    filter: 'Type',
    allTypes: 'All types',
    sort: 'Sort',
    newest: 'Newest first',
    oldest: 'Oldest first',
    loading: 'Loading notifications',
    loadingCopy: 'Your latest admission updates are being retrieved.',
    errorTitle: 'Notifications could not be loaded',
    errorCopy: 'Check your connection and try again.',
    emptyTitle: 'No notifications found',
    emptyCopy: 'New admission updates and messages will appear here.',
    list: 'Notification list',
    inbox: 'Inbox',
    of: 'of',
    noMessage: 'No additional message was provided.',
    unread: 'Unread',
    read: 'Read',
    sent: 'Sent:',
    message: 'Official message',
    pagination: 'Notification pages',
    previous: 'Previous page',
    next: 'Next page',
    markAll: 'Mark all as read',
    markingAll: 'Marking all…',
    readUpdateError: 'The read status could not be updated. Please try again.',
    backToInbox: 'Back to inbox',
    types: {
      None: 'General notification',
      DocumentsRequired: 'Documents required',
      DocumentReplacementRequested: 'Document replacement requested',
      DocumentApproved: 'Document approved',
      DocumentRejected: 'Document rejected',
      ApplicationStatusChanged: 'Application status changed',
    },
    actions: {
      documents: 'Go to Documents',
      replacements: 'Go to Replacement Requests',
      applications: 'Go to Applications',
      support: 'Contact Support',
    },
  },
  ar: {
    eyebrow: 'إشعارات المتقدم',
    title: 'الإشعارات',
    subtitle: 'راجع المراسلات الرسمية وتحديثات طلب التقديم.',
    count: 'إشعار',
    search: 'ابحث في الإشعارات…',
    filter: 'النوع',
    allTypes: 'كل الأنواع',
    sort: 'الترتيب',
    newest: 'الأحدث أولاً',
    oldest: 'الأقدم أولاً',
    loading: 'جارٍ تحميل الإشعارات',
    loadingCopy: 'يتم الآن استرجاع أحدث تحديثات القبول.',
    errorTitle: 'تعذر تحميل الإشعارات',
    errorCopy: 'تحقق من اتصالك ثم حاول مرة أخرى.',
    emptyTitle: 'لا توجد إشعارات',
    emptyCopy: 'ستظهر هنا تحديثات القبول والرسائل الجديدة.',
    list: 'قائمة الإشعارات',
    inbox: 'الوارد',
    of: 'من',
    noMessage: 'لم يتم توفير رسالة إضافية.',
    unread: 'غير مقروء',
    read: 'مقروء',
    sent: 'أُرسل:',
    message: 'الرسالة الرسمية',
    pagination: 'صفحات الإشعارات',
    previous: 'الصفحة السابقة',
    next: 'الصفحة التالية',
    markAll: 'تحديد الكل كمقروء',
    markingAll: 'جارٍ تحديد الكل…',
    readUpdateError: 'تعذر تحديث حالة القراءة. يرجى المحاولة مرة أخرى.',
    backToInbox: 'العودة إلى الوارد',
    types: {
      None: 'إشعار عام',
      DocumentsRequired: 'مستندات مطلوبة',
      DocumentReplacementRequested: 'مطلوب استبدال مستند',
      DocumentApproved: 'تم قبول المستند',
      DocumentRejected: 'تم رفض المستند',
      ApplicationStatusChanged: 'تغيرت حالة الطلب',
    },
    actions: {
      documents: 'الذهاب إلى المستندات',
      replacements: 'الذهاب إلى طلبات الاستبدال',
      applications: 'الذهاب إلى الطلبات',
      support: 'التواصل مع الدعم',
    },
  },
};
