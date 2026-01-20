import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { errorInterceptor, skipErrorNotification } from './error.interceptor';
import { NotificationService } from '../services/notification.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let notifications: { error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    notifications = { error: vi.fn(), success: vi.fn(), info: vi.fn() } as never;
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notifications },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('shows a toast and rethrows on a 500', () => {
    const onError = vi.fn();
    http.get('/api/customers').subscribe({ error: onError });

    httpMock.expectOne('/api/customers').flush('fail', { status: 500, statusText: 'Server Error' });

    expect(notifications.error).toHaveBeenCalledTimes(1);
    expect(onError).toHaveBeenCalled();
  });

  it('stays silent on a 401 (owned by the auth interceptor)', () => {
    http.get('/api/customers').subscribe({ error: () => {} });

    httpMock.expectOne('/api/customers').flush('nope', { status: 401, statusText: 'Unauthorized' });

    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('stays silent when the request opts out via skipErrorNotification()', () => {
    http.get('/api/leaves', { context: skipErrorNotification() }).subscribe({ error: () => {} });

    httpMock.expectOne('/api/leaves').flush('bad', { status: 400, statusText: 'Bad Request' });

    expect(notifications.error).not.toHaveBeenCalled();
  });
});
