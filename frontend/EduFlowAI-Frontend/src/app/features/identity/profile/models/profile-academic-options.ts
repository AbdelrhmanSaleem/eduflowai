export type LocalizedAcademicOption = {
  value: string;
  en: string;
  ar: string;
};

export const EGYPTIAN_UNIVERSITIES: readonly LocalizedAcademicOption[] = [
  { value: 'Cairo University', en: 'Cairo University', ar: 'جامعة القاهرة' },
  {
    value: 'Ain Shams University',
    en: 'Ain Shams University',
    ar: 'جامعة عين شمس',
  },
  {
    value: 'Alexandria University',
    en: 'Alexandria University',
    ar: 'جامعة الإسكندرية',
  },
  {
    value: 'Mansoura University',
    en: 'Mansoura University',
    ar: 'جامعة المنصورة',
  },
  { value: 'Helwan University', en: 'Helwan University', ar: 'جامعة حلوان' },
  { value: 'Assiut University', en: 'Assiut University', ar: 'جامعة أسيوط' },
  {
    value: 'Zagazig University',
    en: 'Zagazig University',
    ar: 'جامعة الزقازيق',
  },
  { value: 'Tanta University', en: 'Tanta University', ar: 'جامعة طنطا' },
  { value: 'Benha University', en: 'Benha University', ar: 'جامعة بنها' },
  {
    value: 'Suez Canal University',
    en: 'Suez Canal University',
    ar: 'جامعة قناة السويس',
  },
];

export const FACULTIES: readonly LocalizedAcademicOption[] = [
  { value: 'Engineering', en: 'Engineering', ar: 'الهندسة' },
  {
    value: 'Computers and Artificial Intelligence',
    en: 'Computers and Artificial Intelligence',
    ar: 'الحاسبات والذكاء الاصطناعي',
  },
  {
    value: 'Computers and Information',
    en: 'Computers and Information',
    ar: 'الحاسبات والمعلومات',
  },
  { value: 'Science', en: 'Science', ar: 'العلوم' },
  { value: 'Commerce', en: 'Commerce', ar: 'التجارة' },
  { value: 'Business', en: 'Business', ar: 'إدارة الأعمال' },
  { value: 'Arts', en: 'Arts', ar: 'الآداب' },
  { value: 'Education', en: 'Education', ar: 'التربية' },
  { value: 'Agriculture', en: 'Agriculture', ar: 'الزراعة' },
];

export const DEGREE_LEVELS: readonly LocalizedAcademicOption[] = [
  { value: 'Bachelor', en: "Bachelor's degree", ar: 'بكالوريوس' },
  {
    value: 'Postgraduate Diploma',
    en: 'Postgraduate diploma',
    ar: 'دبلوم دراسات عليا',
  },
  { value: 'Master', en: "Master's degree", ar: 'ماجستير' },
  { value: 'Doctorate', en: 'Doctorate', ar: 'دكتوراه' },
];

export const MAJORS: readonly LocalizedAcademicOption[] = [
  { value: 'Computer Science', en: 'Computer Science', ar: 'علوم الحاسب' },
  {
    value: 'Information Systems',
    en: 'Information Systems',
    ar: 'نظم المعلومات',
  },
  {
    value: 'Information Technology',
    en: 'Information Technology',
    ar: 'تكنولوجيا المعلومات',
  },
  {
    value: 'Software Engineering',
    en: 'Software Engineering',
    ar: 'هندسة البرمجيات',
  },
  {
    value: 'Artificial Intelligence',
    en: 'Artificial Intelligence',
    ar: 'الذكاء الاصطناعي',
  },
  { value: 'Data Science', en: 'Data Science', ar: 'علوم البيانات' },
  {
    value: 'Computer Engineering',
    en: 'Computer Engineering',
    ar: 'هندسة الحاسبات',
  },
  {
    value: 'Communications Engineering',
    en: 'Communications Engineering',
    ar: 'هندسة الاتصالات',
  },
  {
    value: 'Electronics Engineering',
    en: 'Electronics Engineering',
    ar: 'هندسة الإلكترونيات',
  },
  {
    value: 'Electrical Engineering',
    en: 'Electrical Engineering',
    ar: 'الهندسة الكهربائية',
  },
  {
    value: 'Business Information Systems',
    en: 'Business Information Systems',
    ar: 'نظم معلومات الأعمال',
  },
];
