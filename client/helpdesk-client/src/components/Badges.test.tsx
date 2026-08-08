import {render,screen} from '@testing-library/react'
import {describe,expect,it} from 'vitest'
import {CancelledBadge,TicketPriorityBadge,TicketStatusBadge} from './Badges'
describe('semantic badges',()=>{it('maps known names without lookup IDs',()=>{render(<><TicketStatusBadge name="Closed"/><TicketPriorityBadge name="Critical"/></>);expect(screen.getByText('Closed')).toHaveClass('badge-success');expect(screen.getByText('Critical')).toHaveClass('badge-danger')});it('uses a readable neutral fallback for unknown names',()=>{render(<TicketStatusBadge name="Awaiting vendor"/>);expect(screen.getByText('Awaiting vendor')).toHaveClass('badge-neutral')});it('labels cancellation with text',()=>{render(<CancelledBadge/>);expect(screen.getByText('Cancelled')).toHaveClass('badge-danger')})})
