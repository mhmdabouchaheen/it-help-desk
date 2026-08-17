import{render,screen}from'@testing-library/react'
import{describe,expect,it,vi}from'vitest'
import{AppErrorBoundary}from'./AppErrorBoundary'
function Broken():never{throw new Error('private implementation detail')}
describe('AppErrorBoundary',()=>{it('shows a safe recovery screen without exception details',()=>{vi.spyOn(console,'error').mockImplementation(()=>undefined);render(<AppErrorBoundary><Broken/></AppErrorBoundary>);expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong');expect(screen.getByRole('button',{name:'Reload application'})).toBeInTheDocument();expect(screen.queryByText(/private implementation detail/)).not.toBeInTheDocument()})})
