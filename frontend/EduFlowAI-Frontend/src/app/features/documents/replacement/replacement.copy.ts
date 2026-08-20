import type { AppLocale } from '../../../core/i18n/locale.store';
import type { ReplacementRequestStatus } from './models/replacement-request.model';

interface ReplacementCopy {
  common: {
    replacementRequests: string;
    retry: string;
    statuses: Record<ReplacementRequestStatus, string>;
    documentTypes: Record<string, string>;
    unknownDocument: string;
  };
  list: {
    eyebrow: string;
    title: string;
    subtitle: string;
    count: string;
    search: string;
    status: string;
    allStatuses: string;
    sort: string;
    newest: string;
    oldest: string;
    loading: string;
    loadingCopy: string;
    errorTitle: string;
    errorCopy: string;
    emptyTitle: string;
    emptyCopy: string;
    noResultsTitle: string;
    noResultsCopy: string;
    clearFilters: string;
    requested: string;
    openRequest: string;
  };
  details: {
    breadcrumb: string;
    titlePrefix: string;
    subtitle: string;
    loading: string;
    errorTitle: string;
    errorCopy: string;
    backToRequests: string;
    currentStatus: string;
    reviewerFeedback: string;
    requestSummary: string;
    documentType: string;
    requestedOn: string;
    requestId: string;
    uploadTitle: string;
    uploadFormats: string;
    dropFile: string;
    or: string;
    browse: string;
    selectedFile: string;
    remove: string;
    note: string;
    cancel: string;
    submit: string;
    submitting: string;
    fulfilledTitle: string;
    fulfilledCopy: string;
    uploadSuccess: string;
    uploadError: string;
    invalidType: string;
    fileTooLarge: string;
    emptyFile: string;
  };
}

const shared = {
  en: {
    replacementRequests: 'Replacement Requests',
    retry: 'Try again',
    statuses: {
      None: 'Unknown',
      Open: 'Replacement requested',
      Fulfilled: 'Fulfilled',
    },
    documentTypes: {
      NationalId: 'National ID',
      BirthCertificate: 'Birth certificate',
      GraduationCertificate: 'Graduation certificate',
      MilitaryCertificate: 'Military certificate',
    },
    unknownDocument: 'Document',
  },
  ar: {
    replacementRequests: 'طلبات استبدال المستندات',
    retry: 'حاول مرة أخرى',
    statuses: {
      None: 'غير معروف',
      Open: 'مطلوب الاستبدال',
      Fulfilled: 'تم الاستبدال',
    },
    documentTypes: {
      NationalId: 'بطاقة الرقم القومي',
      BirthCertificate: 'شهادة الميلاد',
      GraduationCertificate: 'شهادة التخرج',
      MilitaryCertificate: 'شهادة الموقف من التجنيد',
    },
    unknownDocument: 'المستند',
  },
} as const;

export const REPLACEMENT_COPY: Record<AppLocale, ReplacementCopy> = {
  en: {
    common: shared.en,
    list: {
      eyebrow: 'Document follow-up',
      title: 'Replacement Requests',
      subtitle: 'Review documents that require a new version and track their progress.',
      count: 'requests',
      search: 'Search by document name…',
      status: 'Status',
      allStatuses: 'All statuses',
      sort: 'Sort',
      newest: 'Newest first',
      oldest: 'Oldest first',
      loading: 'Loading replacement requests',
      loadingCopy: 'We are retrieving your document requests.',
      errorTitle: 'Replacement requests could not be loaded',
      errorCopy: 'Check your connection and try again.',
      emptyTitle: 'No replacement requests',
      emptyCopy: 'Requests from the review team will appear here when a document must be replaced.',
      noResultsTitle: 'No matching requests',
      noResultsCopy: 'Try changing your search or status filter.',
      clearFilters: 'Clear filters',
      requested: 'Requested',
      openRequest: 'View request',
    },
    details: {
      breadcrumb: 'Replacement Requests',
      titlePrefix: 'Document Replacement Request:',
      subtitle: 'Review the feedback and upload a new version of your document.',
      loading: 'Loading replacement request…',
      errorTitle: 'This replacement request could not be loaded',
      errorCopy: 'The request may be unavailable or you may not have access to it.',
      backToRequests: 'Back to replacement requests',
      currentStatus: 'Current Status',
      reviewerFeedback: 'Reviewer feedback',
      requestSummary: 'Request Summary',
      documentType: 'Document type',
      requestedOn: 'Requested on',
      requestId: 'Request ID',
      uploadTitle: 'Upload Replacement',
      uploadFormats: 'Accepted formats: PDF, JPG, JPEG, PNG (Max 10MB)',
      dropFile: 'Drag and drop your file here',
      or: 'or',
      browse: 'Browse files',
      selectedFile: 'Selected file',
      remove: 'Remove file',
      note: 'A “Fulfilled” status means the replacement was received and is awaiting a new review, not final approval.',
      cancel: 'Cancel',
      submit: 'Submit replacement',
      submitting: 'Submitting…',
      fulfilledTitle: 'Replacement received',
      fulfilledCopy: 'A new version was already submitted for this request and is being reviewed.',
      uploadSuccess: 'Your replacement was uploaded and verification has started.',
      uploadError: 'The replacement could not be uploaded. Please try again.',
      invalidType: 'Choose a PDF, JPG, JPEG, or PNG file.',
      fileTooLarge: 'The file must be 10MB or smaller.',
      emptyFile: 'The selected file is empty.',
    },
  },
  ar: {
    common: shared.ar,
    list: {
      eyebrow: 'متابعة المستندات',
      title: 'طلبات استبدال المستندات',
      subtitle: 'راجع المستندات التي تتطلب نسخة جديدة وتابع تقدمها.',
      count: 'طلب',
      search: 'ابحث باسم المستند…',
      status: 'الحالة',
      allStatuses: 'كل الحالات',
      sort: 'الترتيب',
      newest: 'الأحدث أولاً',
      oldest: 'الأقدم أولاً',
      loading: 'جارٍ تحميل طلبات الاستبدال',
      loadingCopy: 'يتم الآن استرجاع طلبات المستندات الخاصة بك.',
      errorTitle: 'تعذر تحميل طلبات الاستبدال',
      errorCopy: 'تحقق من اتصالك ثم حاول مرة أخرى.',
      emptyTitle: 'لا توجد طلبات استبدال',
      emptyCopy: 'ستظهر هنا طلبات فريق المراجعة عندما يلزم استبدال أحد المستندات.',
      noResultsTitle: 'لا توجد طلبات مطابقة',
      noResultsCopy: 'حاول تغيير البحث أو تصفية الحالة.',
      clearFilters: 'مسح عوامل التصفية',
      requested: 'تاريخ الطلب',
      openRequest: 'عرض الطلب',
    },
    details: {
      breadcrumb: 'طلبات الاستبدال',
      titlePrefix: 'طلب استبدال مستند:',
      subtitle: 'راجع ملاحظات المراجع وارفع نسخة جديدة من مستندك.',
      loading: 'جارٍ تحميل طلب الاستبدال…',
      errorTitle: 'تعذر تحميل طلب الاستبدال',
      errorCopy: 'قد يكون الطلب غير متاح أو قد لا يكون لديك صلاحية الوصول إليه.',
      backToRequests: 'العودة إلى طلبات الاستبدال',
      currentStatus: 'الحالة الحالية',
      reviewerFeedback: 'ملاحظات المراجع',
      requestSummary: 'ملخص الطلب',
      documentType: 'نوع المستند',
      requestedOn: 'تاريخ الطلب',
      requestId: 'رقم الطلب',
      uploadTitle: 'رفع المستند البديل',
      uploadFormats: 'الصيغ المقبولة: PDF وJPG وJPEG وPNG (بحد أقصى 10 ميجابايت)',
      dropFile: 'اسحب الملف وأفلته هنا',
      or: 'أو',
      browse: 'اختيار ملف',
      selectedFile: 'الملف المحدد',
      remove: 'إزالة الملف',
      note: 'تعني حالة «تم الاستبدال» أن المستند استُلم وينتظر مراجعة جديدة، وليست موافقة نهائية.',
      cancel: 'إلغاء',
      submit: 'إرسال المستند البديل',
      submitting: 'جارٍ الإرسال…',
      fulfilledTitle: 'تم استلام المستند البديل',
      fulfilledCopy: 'تم إرسال نسخة جديدة لهذا الطلب وهي قيد المراجعة.',
      uploadSuccess: 'تم رفع المستند البديل وبدأت عملية التحقق.',
      uploadError: 'تعذر رفع المستند البديل. يرجى المحاولة مرة أخرى.',
      invalidType: 'اختر ملف PDF أو JPG أو JPEG أو PNG.',
      fileTooLarge: 'يجب ألا يزيد حجم الملف عن 10 ميجابايت.',
      emptyFile: 'الملف المحدد فارغ.',
    },
  },
};
