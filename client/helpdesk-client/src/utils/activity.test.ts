import {describe,expect,it} from 'vitest'
import {friendlyActivityLabel} from './activity'

describe('friendlyActivityLabel',()=>{
  it('uses concise support-domain labels for known activity actions',()=>{
    expect(friendlyActivityLabel('ticket.internal_comment_added')).toBe('Internal note added')
  })

  it('turns unknown machine-readable actions into readable labels',()=>{
    expect(friendlyActivityLabel('account.password_changed')).toBe('Account Password Changed')
  })
})
