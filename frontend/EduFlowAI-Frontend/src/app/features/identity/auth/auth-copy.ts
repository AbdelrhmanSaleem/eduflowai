import { Injectable, inject } from '@angular/core';

import { LocaleStore } from '../../../core/i18n/locale.store';

const AUTH_COPY = {
  en: {
    brand: 'ITI Admissions',
    welcomeBack: 'Welcome back. Please log in to continue.',
    createAccount: 'Create your account to start your application.',
    accountRecovery: 'Account Recovery',
    email: 'Email Address',
    emailPlaceholder: 'applicant@example.com',
    password: 'Password',
    confirmPassword: 'Confirm Password',
    newPassword: 'New Password',
    confirmNewPassword: 'Confirm New Password',
    showPassword: 'Show password',
    hidePassword: 'Hide password',
    login: 'Log in',
    loggingIn: 'Logging in…',
    forgotPassword: 'Forgot password?',
    noAccount: "Don't have an account?",
    register: 'Register',
    registering: 'Creating account…',
    haveAccount: 'Already have an account?',
    passwordRequirements: 'Password requirements',
    passwordLength: 'At least 8 characters',
    passwordUppercase: 'One uppercase letter',
    passwordLowercase: 'One lowercase letter',
    passwordDigit: 'One number',
    required: 'This field is required.',
    invalidEmail: 'Enter a valid email address.',
    passwordsMismatch: 'The passwords do not match.',
    invalidCredentials: 'Invalid email or password.',
    accountUnconfirmed:
      'Confirm your email address before signing in.',
    accountInactive:
      'This account is inactive. Contact support if you believe this is a mistake.',
    accountUnavailable:
      'Confirm your email before signing in, or contact support if your account is inactive.',
    accountLocked:
      'Your account is temporarily locked. Please try again later.',
    rateLimited:
      'Too many attempts were made. Please wait and try again.',
    unexpected:
      'We could not complete your request. Check your connection and try again.',
    duplicateEmail: 'An account with this email already exists.',
    registrationInvalid:
      'Review the information below and try creating your account again.',
    registrationSuccess: 'Check your email',
    registrationSuccessBody:
      'Your account was created. Use the confirmation link we sent to your email before signing in.',
    developmentOnly: 'Local development only',
    confirmUsingToken: 'Confirm this account with the development token',
    backToLogin: 'Back to login',
    confirmationChecking: 'Confirming your email…',
    confirmationSuccess: 'Email confirmed',
    confirmationSuccessBody:
      'Your email is verified. You can now log in to your account.',
    confirmationInvalid: 'This confirmation link is invalid or has expired.',
    confirmationInvalidBody:
      'Open the latest link from your email, or register again if you no longer have one.',
    confirmationTechnicalFailure: 'We could not confirm your email',
    confirmationTechnicalFailureBody:
      'The link may still be valid. Check your connection and try again.',
    retry: 'Try this link again',
    goToLogin: 'Continue to login',
    forgotTitle: 'Forgot your password?',
    forgotBody:
      "Enter the email address associated with your account and we'll send you a reset link.",
    sendReset: 'Send reset link',
    sending: 'Sending…',
    emailSent: 'Email sent',
    emailSentBody:
      'If an account matches that address, a password reset link is on its way. Please also check your spam folder.',
    resend: 'Send another email',
    useDevelopmentReset: 'Open the local reset link',
    resetTitle: 'Create a new password',
    resetBody:
      'Choose a strong password that you have not used for this account before.',
    resetPassword: 'Reset password',
    resetting: 'Updating password…',
    resetSuccess: 'Password updated',
    resetSuccessBody:
      'Your password has been changed. You can now sign in with your new password.',
    resetInvalid: 'Link expired or invalid',
    resetInvalidBody:
      'This reset link can no longer be used. Request a new link to continue.',
    requestNewLink: 'Request a new link',
  },
  ar: {
    brand: 'قبول معهد تكنولوجيا المعلومات',
    welcomeBack: 'مرحبًا بعودتك. سجّل الدخول للمتابعة.',
    createAccount: 'أنشئ حسابك لبدء طلب التقديم.',
    accountRecovery: 'استعادة الحساب',
    email: 'البريد الإلكتروني',
    emailPlaceholder: 'applicant@example.com',
    password: 'كلمة المرور',
    confirmPassword: 'تأكيد كلمة المرور',
    newPassword: 'كلمة المرور الجديدة',
    confirmNewPassword: 'تأكيد كلمة المرور الجديدة',
    showPassword: 'إظهار كلمة المرور',
    hidePassword: 'إخفاء كلمة المرور',
    login: 'تسجيل الدخول',
    loggingIn: 'جارٍ تسجيل الدخول…',
    forgotPassword: 'نسيت كلمة المرور؟',
    noAccount: 'ليس لديك حساب؟',
    register: 'إنشاء حساب',
    registering: 'جارٍ إنشاء الحساب…',
    haveAccount: 'لديك حساب بالفعل؟',
    passwordRequirements: 'متطلبات كلمة المرور',
    passwordLength: 'ثمانية أحرف على الأقل',
    passwordUppercase: 'حرف إنجليزي كبير واحد',
    passwordLowercase: 'حرف إنجليزي صغير واحد',
    passwordDigit: 'رقم واحد',
    required: 'هذا الحقل مطلوب.',
    invalidEmail: 'أدخل بريدًا إلكترونيًا صحيحًا.',
    passwordsMismatch: 'كلمتا المرور غير متطابقتين.',
    invalidCredentials: 'البريد الإلكتروني أو كلمة المرور غير صحيحة.',
    accountUnconfirmed:
      'أكّد بريدك الإلكتروني قبل تسجيل الدخول.',
    accountInactive:
      'هذا الحساب غير نشط. تواصل مع الدعم إذا كنت تعتقد أن هناك خطأ.',
    accountUnavailable:
      'أكّد بريدك الإلكتروني قبل الدخول، أو تواصل مع الدعم إذا كان الحساب غير نشط.',
    accountLocked: 'الحساب مقفل مؤقتًا. حاول مرة أخرى لاحقًا.',
    rateLimited: 'عدد المحاولات كبير. انتظر قليلًا ثم حاول مرة أخرى.',
    unexpected:
      'تعذر إكمال الطلب. تحقق من اتصالك وحاول مرة أخرى.',
    duplicateEmail: 'يوجد حساب مسجل بهذا البريد الإلكتروني.',
    registrationInvalid: 'راجع البيانات وحاول إنشاء الحساب مرة أخرى.',
    registrationSuccess: 'تحقق من بريدك الإلكتروني',
    registrationSuccessBody:
      'تم إنشاء حسابك. استخدم رابط التأكيد المرسل إلى بريدك قبل تسجيل الدخول.',
    developmentOnly: 'لبيئة التطوير المحلية فقط',
    confirmUsingToken: 'تأكيد الحساب باستخدام رمز التطوير',
    backToLogin: 'العودة إلى تسجيل الدخول',
    confirmationChecking: 'جارٍ تأكيد بريدك…',
    confirmationSuccess: 'تم تأكيد البريد الإلكتروني',
    confirmationSuccessBody:
      'تم التحقق من بريدك ويمكنك الآن تسجيل الدخول إلى حسابك.',
    confirmationInvalid: 'رابط التأكيد غير صالح أو انتهت صلاحيته.',
    confirmationInvalidBody:
      'استخدم أحدث رابط في بريدك، أو أنشئ حسابًا جديدًا إن لم يعد الرابط متاحًا.',
    confirmationTechnicalFailure: 'تعذر تأكيد بريدك الإلكتروني',
    confirmationTechnicalFailureBody:
      'قد يظل الرابط صالحًا. تحقق من اتصالك ثم حاول مرة أخرى.',
    retry: 'إعادة محاولة فتح الرابط',
    goToLogin: 'المتابعة لتسجيل الدخول',
    forgotTitle: 'هل نسيت كلمة المرور؟',
    forgotBody:
      'أدخل البريد المرتبط بحسابك وسنرسل لك رابطًا لإعادة تعيين كلمة المرور.',
    sendReset: 'إرسال رابط الاستعادة',
    sending: 'جارٍ الإرسال…',
    emailSent: 'تم إرسال البريد',
    emailSentBody:
      'إذا وُجد حساب مطابق فسيصلك رابط الاستعادة. تحقق أيضًا من الرسائل غير المرغوب فيها.',
    resend: 'إرسال بريد آخر',
    useDevelopmentReset: 'فتح رابط الاستعادة المحلي',
    resetTitle: 'أنشئ كلمة مرور جديدة',
    resetBody: 'اختر كلمة مرور قوية لم تستخدمها لهذا الحساب من قبل.',
    resetPassword: 'تعيين كلمة المرور',
    resetting: 'جارٍ تحديث كلمة المرور…',
    resetSuccess: 'تم تحديث كلمة المرور',
    resetSuccessBody:
      'تم تغيير كلمة المرور ويمكنك الآن الدخول باستخدام الكلمة الجديدة.',
    resetInvalid: 'الرابط غير صالح أو منتهي الصلاحية',
    resetInvalidBody:
      'لا يمكن استخدام رابط الاستعادة هذا. اطلب رابطًا جديدًا للمتابعة.',
    requestNewLink: 'طلب رابط جديد',
  },
} as const;

export type AuthCopyKey = keyof (typeof AUTH_COPY)['en'];

@Injectable({ providedIn: 'root' })
export class AuthCopy {
  private readonly localeStore = inject(LocaleStore);

  readonly locale = this.localeStore.locale;

  t(key: AuthCopyKey): string {
    return AUTH_COPY[this.locale()][key];
  }
}
