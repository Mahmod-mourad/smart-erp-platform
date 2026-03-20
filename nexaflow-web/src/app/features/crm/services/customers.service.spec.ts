import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CustomersService } from './customers.service';
import { ApiService } from '../../../core/services/api.service';
import { CrmState } from '../../../core/state/crm.state';
import { CustomerDto, CreateCustomerDto } from '../../../shared/models/crm.models';

function customer(overrides: Partial<CustomerDto> = {}): CustomerDto {
  return {
    id: 'c1',
    name: 'Ahmed Trading Co.',
    status: 'Active',
    leadsCount: 0,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

describe('CustomersService', () => {
  let service: CustomersService;
  let state: CrmState;
  let api: {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    api = { get: vi.fn(), post: vi.fn(), delete: vi.fn() };
    TestBed.configureTestingModule({
      providers: [CustomersService, CrmState, { provide: ApiService, useValue: api }],
    });
    service = TestBed.inject(CustomersService);
    state = TestBed.inject(CrmState);
  });

  describe('loadAll', () => {
    it('sets loading, fills state from the API, then clears loading', () => {
      const data = [customer({ id: 'a' }), customer({ id: 'b' })];
      api.get.mockReturnValue(of(data));

      service.loadAll().subscribe();

      expect(api.get).toHaveBeenCalledWith('customers');
      expect(state.customers()).toEqual(data);
      expect(state.isLoading()).toBe(false);
    });

    it('clears loading and rethrows when the API fails', () => {
      api.get.mockReturnValue(throwError(() => new Error('boom')));
      const onError = vi.fn();

      service.loadAll().subscribe({ error: onError });

      expect(onError).toHaveBeenCalled();
      expect(state.isLoading()).toBe(false);
    });
  });

  it('create() posts the dto and prepends the result to state', () => {
    const dto: CreateCustomerDto = { name: 'New Co.' };
    const created = customer({ id: 'new', name: 'New Co.' });
    api.post.mockReturnValue(of(created));
    state.setCustomers([customer({ id: 'existing' })]);

    service.create(dto).subscribe();

    expect(api.post).toHaveBeenCalledWith('customers', dto);
    expect(state.customers().map((c) => c.id)).toEqual(['new', 'existing']);
  });

  it('delete() calls the API and removes the customer from state', () => {
    api.delete.mockReturnValue(of(undefined));
    state.setCustomers([customer({ id: 'c1' }), customer({ id: 'c2' })]);

    service.delete('c1').subscribe();

    expect(api.delete).toHaveBeenCalledWith('customers/c1');
    expect(state.customers().map((c) => c.id)).toEqual(['c2']);
  });
});
