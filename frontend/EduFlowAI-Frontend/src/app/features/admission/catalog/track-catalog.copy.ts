import { Injectable, inject } from '@angular/core';

import { LocaleStore } from '../../../core/i18n/locale.store';

export type TrackCatalogCopyKey =
  | 'eyebrow'
  | 'title'
  | 'subtitle'
  | 'searchLabel'
  | 'searchPlaceholder'
  | 'availableCount'
  | 'loading'
  | 'loadFailed'
  | 'retry'
  | 'emptyTitle'
  | 'emptyDescription'
  | 'noMatchesTitle'
  | 'noMatchesDescription'
  | 'activeIntake'
  | 'activeTrack'
  | 'category'
  | 'allCategories'
  | 'totalHours'
  | 'hours'
  | 'minimumGrade'
  | 'eligibility'
  | 'eligibilityDescription'
  | 'graduationWindow'
  | 'graduationWindowValue'
  | 'prerequisites'
  | 'noPrerequisites'
  | 'branches'
  | 'branchCount'
  | 'locations'
  | 'locationCount'
  | 'noLocations'
  | 'seats'
  | 'noActiveCapacity'
  | 'viewDetails'
  | 'officialTrackPage'
  | 'backToCatalog'
  | 'aboutTrack'
  | 'branchOfferings'
  | 'branchOfferingsDescription'
  | 'officialLocations'
  | 'officialLocationsDescription'
  | 'governorate'
  | 'notProvided'
  | 'notPublished'
  | 'capacity'
  | 'detailsNotFound'
  | 'detailsLoadFailed';

type CopyDictionary = Record<TrackCatalogCopyKey, string>;

const COPY: Record<'en' | 'ar', CopyDictionary> = {
  en: {
    eyebrow: 'Official ITI 9-Month intake',
    title: 'Explore available training tracks',
    subtitle:
      'Compare official ITI track reference information with the branches and capacity currently offered in the active admission cycle.',
    searchLabel: 'Search tracks',
    searchPlaceholder: 'Search by track, category, eligibility, topic, or location',
    availableCount: '{{count}} tracks available',
    loading: 'Loading the active track catalog…',
    loadFailed: 'We could not load the track catalog.',
    retry: 'Try again',
    emptyTitle: 'No tracks are available yet',
    emptyDescription: 'No active-cycle track offerings are available yet.',
    noMatchesTitle: 'No matching tracks',
    noMatchesDescription: 'Try a different track, category, topic, or location.',
    activeIntake: 'Official ITI track',
    activeTrack: 'Active program track',
    category: 'Category',
    allCategories: 'All categories',
    totalHours: 'Total hours',
    hours: '{{count}} hours',
    minimumGrade: 'Official published minimum grade',
    eligibility: 'Official ITI reference eligibility',
    eligibilityDescription: "Reference information from ITI track materials. Application eligibility is enforced separately by the active cycle's common rule.",
    graduationWindow: 'Official published graduation window',
    graduationWindowValue: 'Within the last {{count}} years',
    prerequisites: 'Recommended topics',
    noPrerequisites: 'No prerequisite topics listed',
    branches: 'Branches',
    branchCount: '{{count}} branch offerings',
    locations: 'Locations',
    locationCount: '{{count}} locations',
    noLocations: 'No locations available',
    seats: '{{count}} seats',
    noActiveCapacity: 'No active-cycle capacity configured',
    viewDetails: 'View track details',
    officialTrackPage: 'Official ITI page',
    backToCatalog: 'Back to track catalog',
    aboutTrack: 'About this track',
    branchOfferings: 'Current branch offerings',
    branchOfferingsDescription: 'Capacity comes from the active admission-cycle offerings.',
    officialLocations: 'Official delivery locations',
    officialLocationsDescription: 'These locations come from the official ITI Intake 47 catalog.',
    governorate: 'Governorate',
    notProvided: 'Not specified',
    notPublished: 'Not published',
    capacity: 'Capacity',
    detailsNotFound: 'This track is not available in the catalog.',
    detailsLoadFailed: 'We could not load this track.',
  },
  ar: {
    eyebrow: 'برنامج التسعة أشهر الرسمي من معهد تكنولوجيا المعلومات',
    title: 'استكشف المسارات التدريبية المتاحة',
    subtitle: 'قارن معلومات المسار المرجعية المنشورة من ITI بالفروع والسعة المتاحة حاليًا في دورة التقديم النشطة.',
    searchLabel: 'البحث في المسارات',
    searchPlaceholder: 'ابحث بالمسار أو الفئة أو شروط ITI المرجعية للمسار أو الموضوع أو الموقع',
    availableCount: '{{count}} مسارات متاحة',
    loading: 'جارٍ تحميل قائمة المسارات النشطة…',
    loadFailed: 'تعذر تحميل قائمة المسارات.',
    retry: 'إعادة المحاولة',
    emptyTitle: 'لا توجد مسارات متاحة حاليًا',
    emptyDescription: 'لا توجد عروض مسارات متاحة حاليًا في دورة تقديم نشطة.',
    noMatchesTitle: 'لا توجد نتائج مطابقة',
    noMatchesDescription: 'جرّب مسارًا أو فئة أو موضوعًا أو موقعًا آخر.',
    activeIntake: 'مسار رسمي من معهد تكنولوجيا المعلومات',
    activeTrack: 'مسار نشط في البرنامج',
    category: 'الفئة',
    allCategories: 'كل الفئات',
    totalHours: 'إجمالي الساعات',
    hours: '{{count}} ساعة',
    minimumGrade: 'الحد الأدنى المنشور رسميًا للتقدير',
    eligibility: 'شروط ITI المرجعية للمسار',
    eligibilityDescription: 'معلومات مرجعية من مواد ITI الرسمية. أهلية التقديم الفعلية يحددها بشكل منفصل شرط دورة التقديم النشطة المشترك.',
    graduationWindow: 'نطاق سنة التخرج المنشور رسميًا',
    graduationWindowValue: 'خلال آخر {{count}} سنوات',
    prerequisites: 'الموضوعات الموصى بها',
    noPrerequisites: 'لا توجد موضوعات تمهيدية مسجلة',
    branches: 'الفروع',
    branchCount: '{{count}} فروع متاحة',
    locations: 'المواقع',
    locationCount: '{{count}} مواقع',
    noLocations: 'لا توجد مواقع متاحة',
    seats: '{{count}} مقعدًا',
    noActiveCapacity: 'لم تُحدد سعة لدورة تقديم نشطة',
    viewDetails: 'عرض تفاصيل المسار',
    officialTrackPage: 'صفحة معهد تكنولوجيا المعلومات الرسمية',
    backToCatalog: 'العودة إلى قائمة المسارات',
    aboutTrack: 'عن المسار',
    branchOfferings: 'الفروع المتاحة حاليًا',
    branchOfferingsDescription: 'تأتي السعة من عروض الفروع في دورة التقديم النشطة.',
    officialLocations: 'مواقع التدريب الرسمية',
    officialLocationsDescription:
      'هذه المواقع مأخوذة من قائمة معهد تكنولوجيا المعلومات الرسمية للدفعة 47.',
    governorate: 'المحافظة',
    notProvided: 'غير محدد',
    notPublished: 'غير منشور',
    capacity: 'السعة',
    detailsNotFound: 'هذا المسار غير متاح في القائمة.',
    detailsLoadFailed: 'تعذر تحميل بيانات هذا المسار.',
  },
};

@Injectable({ providedIn: 'root' })
export class TrackCatalogCopy {
  private readonly locale = inject(LocaleStore);

  readonly isRtl = this.locale.isRtl;

  text(key: TrackCatalogCopyKey, params?: Record<string, string | number>): string {
    const locale = this.locale.locale();
    const value = COPY[locale][key];

    if (!params) {
      return value;
    }

    return Object.entries(params).reduce(
      (result, [name, replacement]) => result.replaceAll(`{{${name}}}`, String(replacement)),
      value,
    );
  }
}
