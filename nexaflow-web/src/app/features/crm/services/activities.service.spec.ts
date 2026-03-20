import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ActivitiesService } from './activities.service';
import { ApiService } from '../../../core/services/api.service';
import { ActivityDto, CreateActivityDto } from '../../../shared/models/crm.models';

describe('ActivitiesService', () => {
  let service: ActivitiesService;
  let api: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { get: vi.fn(), post: vi.fn() };
    TestBed.configureTestingModule({
      providers: [ActivitiesService, { provide: ApiService, useValue: api }],
    });
    service = TestBed.inject(ActivitiesService);
  });

  it('getForCustomer() requests the scoped activity endpoint', () => {
    const activities: ActivityDto[] = [
      {
        id: 'a1',
        customerId: 'c1',
        type: 'Call',
        content: 'Followed up',
        createdAt: '2026-01-01T00:00:00Z',
      },
    ];
    api.get.mockReturnValue(of(activities));
    const received = vi.fn();

    service.getForCustomer('c1').subscribe(received);

    expect(api.get).toHaveBeenCalledWith('customers/c1/activities');
    expect(received).toHaveBeenCalledWith(activities);
  });

  it('create() posts to the scoped endpoint', () => {
    const dto: CreateActivityDto = { type: 'Note', content: 'Discovery call booked' };
    api.post.mockReturnValue(
      of({ id: 'a2', customerId: 'c1', ...dto, createdAt: '2026-01-02T00:00:00Z' }),
    );

    service.create('c1', dto).subscribe();

    expect(api.post).toHaveBeenCalledWith('customers/c1/activities', dto);
  });
});
