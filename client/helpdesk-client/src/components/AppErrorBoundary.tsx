import {Component,type ReactNode} from 'react'

interface Props{children:ReactNode}
interface State{failed:boolean}

/** Keeps unexpected render failures from exposing implementation details to users. */
export class AppErrorBoundary extends Component<Props,State>{
  state:State={failed:false}
  static getDerivedStateFromError():State{return{failed:true}}
  componentDidCatch(){/* Error reporting can be added without serializing user or token state. */}
  render(){if(this.state.failed)return <main className="auth-page"><section className="auth-card" role="alert"><h1>Something went wrong</h1><p>The application encountered an unexpected error. Reload to try again.</p><button type="button" onClick={()=>window.location.reload()}>Reload application</button></section></main>;return this.props.children}
}
