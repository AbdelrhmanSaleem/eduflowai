// The Final & Bulletproof Error Extractor
export function extractErrorMessage(err: any, fallbackMessage: string): string {
    if (!err) return fallbackMessage;
    
    try {
      // --- THE MISSING LINK (For Custom Interceptor Wrapped Errors) ---
      // Extracting the message from err.original.error.message
      if (err.original && err.original.error && err.original.error.message) {
        return err.original.error.message;
      }
  
      // 1. If err is just a plain string thrown by a service/interceptor
      if (typeof err === 'string') return err;
      
      // 2. Safely extract from HttpErrorResponse (.error property)
      if (err.error) {
        const body = err.error;
        
        // If Angular didn't parse the JSON and returned it as a raw string
        if (typeof body === 'string') {
          try {
            const parsed = JSON.parse(body);
            if (parsed.message) return parsed.message;
            if (parsed.title) return parsed.title;
          } catch {
            return body; // Return the string as is if it's plain text
          }
        }
        
        // If Angular parsed it correctly as an Object
        if (typeof body === 'object') {
          // Aggressively check all common property names
          if (body.message) return body.message;
          if (body.Message) return body.Message;
          if (body.title) return body.title;
          
          // Handle ASP.NET Core Validation Errors (errors dictionary)
          if (body.errors && typeof body.errors === 'object') {
            const firstKey = Object.keys(body.errors)[0];
            if (firstKey && Array.isArray(body.errors[firstKey])) {
              return body.errors[firstKey][0];
            }
          }
        }
      }
  
      // 3. If err is the unwrapped backend Result object directly 
      if (err.isSuccess !== undefined && err.message) {
        return err.message;
      }
      
      // 4. Native JavaScript Error objects or unwrapped errors
      // Ignoring generic interceptor messages like "Invalid request"
      if (err.message && typeof err.message === 'string' && !err.message.includes('Http failure response') && err.message !== 'Invalid request') {
        return err.message;
      }
  
      return fallbackMessage;
      
    } catch (e) {
      return fallbackMessage;
    }
}