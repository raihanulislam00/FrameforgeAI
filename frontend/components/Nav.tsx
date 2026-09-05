'use client';
import Link from 'next/link';
export function Nav(){return <nav className="nav"><Link className="brand" href="/dashboard">frame<span>forge</span> / AI</Link><div className="navlinks"><Link href="/dashboard">Dashboard</Link><Link href="/create">Create</Link><Link href="/videos">Library</Link></div><button className="button ghost" onClick={()=>{localStorage.removeItem('accessToken');location.href='/login'}}>Sign out</button></nav>}
