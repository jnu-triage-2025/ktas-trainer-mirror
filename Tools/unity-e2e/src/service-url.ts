export function serviceUrl(value:string):string {
 const url=new URL(value);
 if(url.username||url.password||url.pathname!=='/'||url.search||url.hash
   ||!(url.protocol==='https:'||(url.protocol==='http:'&&url.hostname==='127.0.0.1')))
   throw new Error('E2E service requires an HTTPS origin or loopback HTTP origin');
 return url.origin;
}
