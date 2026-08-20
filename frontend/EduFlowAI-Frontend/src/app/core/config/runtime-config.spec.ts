import { environment } from '../../../environments/environment';
import { RuntimeConfig } from './runtime-config';

describe('RuntimeConfig', () => {
  const configuredRoot = environment.apiUrl.trim().replace(/\/+$/, '');
  const expectedApiBaseUrl = /\/api$/i.test(configuredRoot)
    ? configuredRoot
    : `${configuredRoot}/api`;

  it('builds the API base URL from the selected environment', () => {
    const config = new RuntimeConfig();

    expect(config.apiBaseUrl).toBe(expectedApiBaseUrl);
  });

  it('trusts only relative or configured absolute API URLs', () => {
    const config = new RuntimeConfig();
    const configuredOrigin = new URL(expectedApiBaseUrl).origin;

    expect(config.isTrustedApiUrl('/api/profile')).toBe(true);
    expect(config.isTrustedApiUrl(`${expectedApiBaseUrl}/profile`)).toBe(true);
    expect(config.isTrustedApiUrl('https://example.com/api/profile')).toBe(false);
    expect(
      config.isTrustedApiUrl(`${configuredOrigin}/outside-api/profile`),
    ).toBe(false);
  });
});
