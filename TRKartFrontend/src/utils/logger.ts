import { v4 as uuidv4 } from 'uuid';

interface LogEntry {
  id: string;
  timestamp: string;
  level: 'info' | 'warn' | 'error' | 'debug';
  component: string;
  action: string;
  message: string;
  data?: any;
  userId?: string;
  sessionId?: string;
}

class Logger {
  private sessionId: string;
  private static instance: Logger;

  private constructor() {
    this.sessionId = this.getSessionId();
  }

  public static getInstance(): Logger {
    if (!Logger.instance) {
      Logger.instance = new Logger();
    }
    return Logger.instance;
  }

  private getSessionId(): string {
    let sessionId = sessionStorage.getItem('sessionId');
    if (!sessionId) {
      sessionId = uuidv4();
      sessionStorage.setItem('sessionId', sessionId);
    }
    return sessionId;
  }

  private getUserId(): string | undefined {
    const userData = localStorage.getItem('user');
    if (userData) {
      try {
        const user = JSON.parse(userData);
        return user.id;
      } catch (e) {
        return undefined;
      }
    }
    return undefined;
  }

  private log(level: LogEntry['level'], component: string, action: string, message: string, data?: any): void {
    const userId = this.getUserId();
    const logEntry: LogEntry = {
      id: uuidv4(),
      timestamp: new Date().toISOString(),
      level,
      component,
      action,
      message,
      data,
      ...(userId && { userId }),
      sessionId: this.sessionId
    };

    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      const logMethod = console[level] || console.log;
      logMethod(`[${logEntry.timestamp}] [${level.toUpperCase()}] [${component}] ${action}: ${message}`, data || '');
    }

    // TODO: Send logs to backend in production
    // this.sendToBackend(logEntry);
  }

  public info(component: string, action: string, message: string, data?: any): void {
    this.log('info', component, action, message, data);
  }

  public warn(component: string, action: string, message: string, data?: any): void {
    this.log('warn', component, action, message, data);
  }

  public error(component: string, action: string, message: string, error?: Error, data?: any): void {
    const errorData = {
      ...data,
      error: {
        name: error?.name,
        message: error?.message,
        stack: process.env.NODE_ENV === 'development' ? error?.stack : undefined,
      },
    };
    this.log('error', component, action, message, errorData);
  }

  public debug(component: string, action: string, message: string, data?: any): void {
    if (process.env.NODE_ENV === 'development') {
      this.log('debug', component, action, message, data);
    }
  }

  // TODO: Implement backend log shipping
  // private async sendToBackend(logEntry: LogEntry): Promise<void> {
  //   try {
  //     await fetch('/api/logs', {
  //       method: 'POST',
  //       headers: {
  //         'Content-Type': 'application/json',
  //       },
  //       body: JSON.stringify(logEntry),
  //     });
  //   } catch (error) {
  //     console.error('Failed to send log to backend:', error);
  //   }
  // }
}

export const logger = Logger.getInstance();
