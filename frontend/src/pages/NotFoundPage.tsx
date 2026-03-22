import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <div className="flex h-screen items-center justify-center bg-gray-50">
      <div className="text-center">
        <p className="text-6xl font-bold text-gray-200">404</p>
        <p className="mt-4 text-lg font-medium text-gray-700">Page not found</p>
        <p className="mt-1 text-sm text-gray-500">The page you&apos;re looking for doesn&apos;t exist.</p>
        <Link to="/dashboard" className="btn-primary mt-6 inline-block">
          Back to Dashboard
        </Link>
      </div>
    </div>
  );
}

