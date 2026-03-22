import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { AppShell } from '@/layouts/AppShell';
import { LoginPage } from '@/pages/LoginPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { AlertsPage } from '@/pages/AlertsPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { PageSpinner } from '@/components/ui/Spinner';

// ── Lazy-loaded modules ───────────────────────────────────────────────────────
const ItemsPage               = lazy(() => import('@/pages/items/ItemsPage'));
const ItemFormPage             = lazy(() => import('@/pages/items/ItemFormPage'));
const ItemImportPage           = lazy(() => import('@/pages/imports/ItemImportPage'));
const PurchasesPage            = lazy(() => import('@/pages/purchases/PurchasesPage'));
const PurchaseFormPage         = lazy(() => import('@/pages/purchases/PurchaseFormPage'));
const PurchaseDetailPage       = lazy(() => import('@/pages/purchases/PurchaseDetailPage'));
const OpeningStockImportPage   = lazy(() => import('@/pages/imports/OpeningStockImportPage'));
const PosPage                  = lazy(() => import('@/pages/pos/PosPage'));

function Lazy({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageSpinner />}>{children}</Suspense>;
}

export const router = createBrowserRouter([
  // ── Public routes ────────────────────────────────────────────────────────
  {
    path: '/login',
    element: <LoginPage />,
  },

  // ── Protected routes (require auth) ─────────────────────────────────────
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
        children: [
          // Redirect / → /dashboard
          { index: true, element: <Navigate to="/dashboard" replace /> },
          {
            path: 'dashboard',
            element: <DashboardPage />,
          },
          {
            path: 'alerts',
            element: <AlertsPage />,
          },
          // ── Items ──────────────────────────────────────────────────────────
          {
            path: 'items',
            element: <Lazy><ItemsPage /></Lazy>,
          },
          {
            path: 'items/new',
            element: <Lazy><ItemFormPage /></Lazy>,
          },
          {
            path: 'items/:id/edit',
            element: <Lazy><ItemFormPage /></Lazy>,
          },
          // ── Imports ────────────────────────────────────────────────────────
          {
            path: 'imports/items',
            element: <Lazy><ItemImportPage /></Lazy>,
          },
          {
            path: 'imports/opening-stock',
            element: <Lazy><OpeningStockImportPage /></Lazy>,
          },
          // ── Purchases ──────────────────────────────────────────────────────
          {
            path: 'purchases',
            element: <Lazy><PurchasesPage /></Lazy>,
          },
          {
            path: 'purchases/new',
            element: <Lazy><PurchaseFormPage /></Lazy>,
          },
          {
            path: 'purchases/:id',
            element: <Lazy><PurchaseDetailPage /></Lazy>,
          },
          // ── POS ────────────────────────────────────────────────────────────
          {
            path: 'pos',
            element: <Lazy><PosPage /></Lazy>,
          },
        ],
      },
    ],
  },

  // ── Not found ────────────────────────────────────────────────────────────
  { path: '*', element: <NotFoundPage /> },
]);

