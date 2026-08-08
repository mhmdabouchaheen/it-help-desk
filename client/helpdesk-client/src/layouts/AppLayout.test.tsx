import {render,screen,within} from '@testing-library/react'
import {MemoryRouter} from 'react-router-dom'
import {describe,expect,it,vi} from 'vitest'
import {AppLayout} from './AppLayout'
let unreadCount=4
vi.mock('../auth/useNotifications',()=>({NotificationsProvider:({children}:{children:React.ReactNode})=><>{children}</>,useNotifications:()=>({unreadCount})}))
vi.mock('../auth/AuthProvider',()=>({useAuth:()=>({user:{displayName:'User'},roles:['Employee'],logout:vi.fn()})}))
describe('AppLayout notifications',()=>{it('shows notification navigation and accessible unread badge',()=>{unreadCount=4;render(<MemoryRouter><AppLayout/></MemoryRouter>);const sidebar=screen.getByRole('complementary',{name:'Application sidebar'});expect(within(sidebar).getByRole('link',{name:/Notifications/})).toHaveAttribute('href','/app/notifications');expect(within(sidebar).getByLabelText('4 unread notifications')).toHaveTextContent('4')});it('hides the badge consistently at zero',()=>{unreadCount=0;render(<MemoryRouter><AppLayout/></MemoryRouter>);const sidebar=screen.getByRole('complementary',{name:'Application sidebar'});expect(within(sidebar).getByRole('link',{name:'Notifications'})).toBeInTheDocument();expect(screen.queryByLabelText(/unread notifications/)).not.toBeInTheDocument()});it('caps the visual badge while retaining the accessible count',()=>{unreadCount=120;render(<MemoryRouter><AppLayout/></MemoryRouter>);const sidebar=screen.getByRole('complementary',{name:'Application sidebar'});expect(within(sidebar).getByLabelText('120 unread notifications')).toHaveTextContent('99+')})})
