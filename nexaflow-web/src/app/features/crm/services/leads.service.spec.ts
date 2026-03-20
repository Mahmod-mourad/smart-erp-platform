import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { LeadsService } from './leads.service';
import { ApiService } from '../../../core/services/api.service';
import { CrmState } from '../../../core/state/crm.state';
import { LeadDto } from '../../../shared/models/crm.models';

function lead(overrides: Partial<LeadDto> = {}): LeadDto {
  return {
    id: 'l1',
    title: 'ERP Deal',
    value: 80000,
    stage: 'Prospect',
    customerId: 'c1',
    customerName: 'Ahmed Trading Co.',
    ...overrides,
  };
}

describe('LeadsService', () => {
  let service: LeadsService;
  let state: CrmState;
  let api: {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    patch: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    api = { get: vi.fn(), post: vi.fn(), patch: vi.fn() };
    TestBed.configureTestingModule({
      providers: [LeadsService, CrmState, { provide: ApiService, useValue: api }],
    });
    service = TestBed.inject(LeadsService);
    state = TestBed.inject(CrmState);
  });

  it('loadAll() fills the leads state', () => {
    const data = [lead({ id: 'a' }), lead({ id: 'b' })];
    api.get.mockReturnValue(of(data));

    service.loadAll().subscribe();

    expect(api.get).toHaveBeenCalledWith('leads');
    expect(state.leads()).toEqual(data);
  });

  it('updateStage() patches the API and moves the lead to the new stage in state', () => {
    state.setLeads([lead({ id: 'l1', stage: 'Prospect' })]);
    api.patch.mockReturnValue(of(lead({ id: 'l1', stage: 'Qualified' })));

    service.updateStage('l1', 'Qualified').subscribe();

    expect(api.patch).toHaveBeenCalledWith('leads/l1/stage', { stage: 'Qualified' });
    expect(state.leads()[0].stage).toBe('Qualified');
  });

  it('leadsByStage groups leads after an update', () => {
    state.setLeads([lead({ id: 'l1', stage: 'Prospect' }), lead({ id: 'l2', stage: 'Prospect' })]);
    api.patch.mockReturnValue(of(lead({ id: 'l2', stage: 'Won' })));

    service.updateStage('l2', 'Won').subscribe();

    const grouped = state.leadsByStage();
    expect(grouped.get('Prospect')?.map((l) => l.id)).toEqual(['l1']);
    expect(grouped.get('Won')?.map((l) => l.id)).toEqual(['l2']);
  });
});
