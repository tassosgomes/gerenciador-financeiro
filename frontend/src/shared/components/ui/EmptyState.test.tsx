import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Wallet } from 'lucide-react';

import { EmptyState } from '@/shared/components/ui/EmptyState';

describe('EmptyState', () => {
  it('renders with icon, title and description', () => {
    render(
      <EmptyState
        icon={Wallet}
        title="No items found"
        description="Add your first item to get started"
      />
    );

    expect(screen.getByText('No items found')).toBeInTheDocument();
    expect(screen.getByText('Add your first item to get started')).toBeInTheDocument();
  });

  it('renders with action button when provided', () => {
    const mockAction = vi.fn();

    render(
      <EmptyState
        icon={Wallet}
        title="No items found"
        description="Add your first item to get started"
        actionLabel="Add Item"
        onAction={mockAction}
      />
    );

    const button = screen.getByRole('button', { name: 'Add Item' });
    expect(button).toBeInTheDocument();
  });

  it('calls onAction when button is clicked', async () => {
    const user = userEvent.setup();
    const mockAction = vi.fn();

    render(
      <EmptyState
        icon={Wallet}
        title="No items found"
        description="Add your first item to get started"
        actionLabel="Add Item"
        onAction={mockAction}
      />
    );

    const button = screen.getByRole('button', { name: 'Add Item' });
    await user.click(button);

    expect(mockAction).toHaveBeenCalledTimes(1);
  });

  it('does not render button when action is not provided', () => {
    render(
      <EmptyState
        icon={Wallet}
        title="No items found"
        description="Add your first item to get started"
      />
    );

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
